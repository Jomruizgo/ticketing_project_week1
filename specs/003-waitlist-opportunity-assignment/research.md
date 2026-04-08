# Research: Asignación de Oportunidad de Lista de Espera

**Feature**: 003-waitlist-opportunity-assignment  
**Date**: 2026-04-07  
**Status**: Complete — todas las decisiones resueltas

---

## R1 — Transición de estados de WaitlistOpportunity

**Decision**: Implementar transiciones como método `TransitionTo` en la entidad de dominio con validación de transiciones permitidas.

**Rationale**: El patrón State (GoF) aplicado como validación de transiciones en la entidad mantiene las reglas de negocio en Domain sin depender de frameworks. Las transiciones permitidas son: `Pending→Active`, `Pending→Failed`, `Active→Consumed`, `Active→Expired`. Cualquier otra transición lanza `InvalidOpportunityTransitionException`. Esto evita condicionales dispersos y centraliza la lógica de ciclo de vida.

**Alternatives considered**:
- Máquina de estados formal con librería externa (Stateless): over-engineering para 5 estados con transiciones simples.
- Validación en el handler: viola SRP y dispersa las reglas de transición.

---

## R2 — Patrón Strategy para priorización FIFO

**Decision**: Definir `IPrioritizationStrategy` en `Domain/Interfaces` con método `WaitlistEntry? SelectNextEligible(IReadOnlyList<WaitlistEntry> activeEntries)`. Implementar `FifoStrategy` en `Infrastructure` que ordena la lista por `EnrolledAt` ascendente y retorna la primera. El handler carga la lista completa desde `IWaitlistEntryRepository.GetActiveEntriesByEventAsync(eventId)` y la pasa a la strategy.

**Rationale**: Alineado con FR-015 (extensible) y con el diseño aprobado en patterns/strategy.md (Planning2.md). La strategy opera en memoria sobre datos ya cargados, no consulta la BD directamente. Esto resuelve el problema de loop infinito cuando la reserva falla: el handler remueve la entry fallida de la lista antes de invocar la strategy de nuevo.

**Alternatives considered**:
- Query directa en el handler: no extensible, viola OCP.
- Implementar múltiples strategies desde el inicio: YAGNI, solo se necesita FIFO ahora.

---

## R3 — Puerto de reserva temporal (ITicketReservationPort)

**Decision**: Definir `ITicketReservationPort` en `Domain/Interfaces` con método `Task<bool> TryReserveForWaitlistAsync(long ticketId, string buyerEmail)`. El adaptador en Infrastructure actualiza el ticket de `released` a `reserved` en la misma BD con bloqueo optimista (`WHERE status = 'released'`).

**Rationale**: La reserva por lista de espera opera sobre tickets en estado `released`, distinto de la reserva directa que opera sobre `available` (spec: Assumption). Retornar `bool` en vez de lanzar excepción porque el fallo de reserva es un resultado de negocio esperado (FR-009), no un error excepcional.

**Alternatives considered**:
- Llamada HTTP al ReservationService: innecesario, la BD es compartida y el CRUD Service tiene acceso directo.
- Reutilizar ITicketRepository existente: viola ISP, el repositorio de tickets tiene responsabilidades de lectura que no aplican aquí.

---

## R4 — Patrón Observer para notificación post-activación

**Decision**: Definir `IOpportunityObserver` en `Domain/Interfaces` con método `Task OnOpportunityActivatedAsync(WaitlistOpportunity opportunity)`. El handler invoca al observer después de transicionar a `active`. El adaptador en Infrastructure publica el evento RabbitMQ `waitlist.opportunity.activated` y el mensaje al delay queue `q.waitlist.opportunity.delay`.

**Rationale**: Alineado con Planning2.md (patrón Observer). Desacopla la lógica de asignación de las acciones post-activación (notificaciones, expiración). Si el negocio agrega canales de notificación futuros, se añaden como observers sin modificar el handler.

**Alternatives considered**:
- Publicar directamente en el handler: viola SRP, acopla el handler a RabbitMQ.
- Eventos de dominio con mediator: over-engineering para un solo observer, no hay MediatR en el proyecto.

---

## R5 — Consumer TicketReleasedConsumer (estructura y ACK/NACK)

**Decision**: Crear `TicketReleasedConsumer` como `BackgroundService` siguiendo el patrón exacto de `TicketStatusConsumer` existente. Configurar `BasicQos(0, 1, false)` para procesamiento secuencial (spec: Q4 clarificación). ACK en éxito y fallos de negocio; NACK con `requeue: false` en fallos técnicos (spec: FR-014).

**Rationale**: `prefetchCount = 1` simplifica la iteración FIFO y elimina conflictos de unicidad entre hilos del mismo consumer (spec: clarificación Q4). La restricción única de BD queda como segunda línea de defensa ante concurrencia externa (múltiples instancias del servicio).

**Alternatives considered**:
- `prefetchCount > 1`: mayor throughput pero complejidad en retry por conflicto de unicidad (analizado y descartado en clarificación Q4).
- Consumer en servicio worker separado: innecesario, el CRUD Service ya tiene infrastructure de consumer y acceso a BD.

---

## R6 — Iteración de elegibles ante fallo de reserva

**Decision**: Cuando la reserva temporal falla para un comprador, el handler itera inmediatamente al siguiente elegible en el mismo ciclo de procesamiento. La oportunidad fallida transiciona a `Failed`. Si no quedan elegibles, retorna `NoEligible` al consumer, que publica `ticket.returned_to_inventory`.

**Rationale**: Alineado con clarificación Q1. Evita que una entrada liberada quede bloqueada sin asignación porque un solo intento falló. El handler implementa un loop: `while (nextEligible != null) { tryReserve → success: return | fail: markFailed, nextEligible = strategy.GetNext() }`.

**Alternatives considered**:
- Dejar en `released` esperando nuevo evento: la entrada podría quedar huérfana indefinidamente si no llega otro `ticket.released`.

---

## R7 — Publicación al delay queue para expiración

**Decision**: El observer publica al `q.waitlist.opportunity.delay` cuando la oportunidad transiciona a `active` (no cuando se crea como `pending`). El payload incluye `opportunityId`. La cola tiene TTL configurable (`WAITLIST_OPPORTUNITY_TTL_MS`, default 900000ms = 15min) y DLX hacia `tickets` con routing key `waitlist.opportunity.expired`.

**Rationale**: Alineado con FR-007 y Planning2.md (topología DLX). El TTL comienza desde activación real, no desde creación. Esto evita que el tiempo de procesamiento de la reserva consuma parte de la vigencia del comprador.

**Alternatives considered**:
- TTL calculado por mensaje (message-level TTL): más flexible pero menos predecible, la cola con TTL fijo es más simple y alineada con el patrón existente de `q.ticket.reserved.delay`.

---

## R8 — Idempotencia ante mensajes duplicados

**Decision**: Antes de buscar elegibles, el handler verifica si ya existe una oportunidad activa para el `ticketId` recibido. Si existe, retorna inmediatamente sin acción (FR-012). La verificación es una consulta simple: `FindActiveByTicketIdAsync(ticketId)`.

**Rationale**: Los mensajes `ticket.released` pueden llegar duplicados por redelivery de RabbitMQ. La verificación temprana en el handler es más eficiente que depender solo de la restricción de BD.

**Alternatives considered**:
- Solo restricción de BD: funciona pero genera excepciones innecesarias en el happy path de duplicados.
- Tabla de idempotencia (outbox): over-engineering para el volumen esperado.
