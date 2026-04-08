# Tasks: Asignación de Oportunidad de Lista de Espera

**Input**: Design documents from `/specs/003-waitlist-opportunity-assignment/`  
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/messaging.md, quickstart.md

**Tests**: Incluidos — el plan exige TDD estricto (TC-HU3-01 a TC-HU3-04).

**Organization**: Tareas agrupadas por user story para habilitar implementación y testing independiente de cada historia.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivos distintos, sin dependencias de tareas incompletas)
- **[Story]**: A qué user story pertenece (US1, US2, US3, US4)
- Rutas exactas de archivo incluidas en cada tarea

---

## Phase 1: Setup (Infraestructura compartida)

**Purpose**: Nuevos puertos, excepción de dominio y método TransitionTo en entidad existente. Prerrequisitos compartidos por todas las user stories.

- [X] T001 Agregar método `TransitionTo(WaitlistOpportunityStatus newStatus)` con validación de transiciones permitidas (Pending→Active, Pending→Failed, Active→Consumed, Active→Expired) a la entidad existente en `crud_service/src/CrudService.Domain/Entities/WaitlistOpportunity.cs`
- [X] T002 [P] Crear excepción `InvalidOpportunityTransitionException` en `crud_service/src/CrudService.Domain/Exceptions/InvalidOpportunityTransitionException.cs`
- [X] T003 [P] Crear interfaz `IPrioritizationStrategy` con método `WaitlistEntry? SelectNextEligible(IReadOnlyList<WaitlistEntry> activeEntries)` en `crud_service/src/CrudService.Domain/Interfaces/IPrioritizationStrategy.cs` — alineado con patterns/strategy.md: la strategy opera en memoria sobre la lista que le pasa el handler, no consulta la BD directamente
- [X] T004 [P] Crear interfaz `ITicketReservationPort` con método `Task<bool> TryReserveForWaitlistAsync(long ticketId, string buyerEmail)` en `crud_service/src/CrudService.Domain/Interfaces/ITicketReservationPort.cs`
- [X] T005 [P] Crear interfaz `IOpportunityObserver` con método `Task OnOpportunityActivatedAsync(WaitlistOpportunity opportunity)` en `crud_service/src/CrudService.Domain/Interfaces/IOpportunityObserver.cs`
- [X] T006 Extender `IWaitlistOpportunityRepository` con métodos `Task<WaitlistOpportunity> AddAsync(WaitlistOpportunity opportunity)`, `Task UpdateAsync(WaitlistOpportunity opportunity)` y `Task<WaitlistOpportunity?> FindActiveByTicketIdAsync(long ticketId)` en `crud_service/src/CrudService.Domain/Interfaces/IWaitlistOpportunityRepository.cs`
- [X] T007 Extender `IWaitlistEntryRepository` con métodos `Task<IReadOnlyList<WaitlistEntry>> GetActiveEntriesByEventAsync(long eventId)` y `Task UpdateStatusAsync(long entryId, WaitlistEntryStatus newStatus)` en `crud_service/src/CrudService.Domain/Interfaces/IWaitlistEntryRepository.cs` — cambia de `FindOldestActiveByEventAsync` (devolvía uno) a `GetActiveEntriesByEventAsync` (devuelve toda la lista activa para que la strategy seleccione en memoria)
- [X] T008 [P] Crear record `AssignOpportunityCommand(long TicketId, long EventId)` en `crud_service/src/CrudService.Application/UseCases/Waitlist/AssignOpportunity/AssignOpportunityCommand.cs`
- [X] T009 [P] Crear enum `AssignOpportunityResultType { Assigned, NoEligible, AllFailed }` y record `AssignOpportunityResult(AssignOpportunityResultType Type, WaitlistOpportunity? Opportunity)` en `crud_service/src/CrudService.Application/UseCases/Waitlist/AssignOpportunity/AssignOpportunityResult.cs`
- [X] T010 [P] Crear interfaz `IAssignOpportunityUseCase` con método `Task<AssignOpportunityResult> HandleAsync(AssignOpportunityCommand command)` en `crud_service/src/CrudService.Application/UseCases/Waitlist/AssignOpportunity/IAssignOpportunityUseCase.cs`
- [X] T011 [P] Crear DTO `TicketReleasedEvent` con propiedades `TicketId`, `EventId`, `ReleasedAt` en `crud_service/src/CrudService.Infrastructure/Messaging/TicketReleasedEvent.cs`
- [X] T012 [P] Crear DTO `OpportunityActivatedEvent` con propiedades `OpportunityId`, `WaitlistEntryId`, `TicketId`, `EventId`, `BuyerEmail`, `ActivatedAt`, `ExpiresAt` en `crud_service/src/CrudService.Infrastructure/Messaging/OpportunityActivatedEvent.cs`

---

## Phase 2: Foundational (Prerrequisitos bloqueantes)

**Purpose**: Adaptadores (Strategy, ReservationPort, Observer, repositorios extendidos, consumer). DEBEN completarse antes de cualquier user story.

**⚠️ CRITICAL**: Ninguna tarea de user story puede comenzar sin completar esta fase

- [X] T013 Implementar `FifoStrategy` (adaptador de `IPrioritizationStrategy`): recibe `IReadOnlyList<WaitlistEntry>`, ordena por `EnrolledAt` ascendente en memoria, retorna la primera o null — en `crud_service/src/CrudService.Infrastructure/Strategies/FifoStrategy.cs` — NO consulta la BD; opera sobre la colección que le pasa el handler. El repo provee `GetActiveEntriesByEventAsync`, la strategy selecciona
- [X] T014 [P] Implementar `TicketReservationAdapter` (adaptador de `ITicketReservationPort`): actualiza ticket de `released` a `reserved` con `WHERE status = 'released'`, retorna `true` si `rowsAffected > 0`, `false` si no — en `crud_service/src/CrudService.Infrastructure/Services/TicketReservationAdapter.cs`
- [X] T015 [P] Implementar `OpportunityActivatedObserver` (adaptador de `IOpportunityObserver`): publica evento `waitlist.opportunity.activated` al exchange `tickets` y mensaje al delay queue `q.waitlist.opportunity.delay` con payload `{ opportunityId }` — en `crud_service/src/CrudService.Infrastructure/Messaging/OpportunityActivatedObserver.cs`
- [X] T016 Extender `WaitlistOpportunityRepository` con `AddAsync`, `UpdateAsync` y `FindActiveByTicketIdAsync` en `crud_service/src/CrudService.Infrastructure/Persistence/Repositories/WaitlistOpportunityRepository.cs`
- [X] T017 Extender `WaitlistEntryRepository` con `GetActiveEntriesByEventAsync` (consulta `WaitlistEntries` con `Status == Active` para el evento, retorna lista completa) y `UpdateStatusAsync` en `crud_service/src/CrudService.Infrastructure/Persistence/Repositories/WaitlistEntryRepository.cs`
- [X] T018 Agregar DOS índices parciales únicos en `TicketingDbContext`: (1) `WaitlistOpportunity.TicketId WHERE status = 'active'` (idempotencia FR-012 — previene dos oportunidades activas para el mismo ticket) y (2) `WaitlistOpportunity.WaitlistEntryId WHERE status = 'active'` (concurrencia Planning2.md — previene dos oportunidades activas para el mismo comprador) en `crud_service/src/CrudService.Infrastructure/Persistence/TicketingDbContext.cs`
- [X] T019 Registrar en DI: `IPrioritizationStrategy → FifoStrategy`, `ITicketReservationPort → TicketReservationAdapter`, `IOpportunityObserver → OpportunityActivatedObserver`, `IAssignOpportunityUseCase → AssignOpportunityHandler` en `crud_service/src/CrudService.Infrastructure/DependencyInjection.cs`
- [X] T020 Crear `TicketReleasedConsumer` como `BackgroundService` con `BasicQos(0, 1, false)`, deserialización de `TicketReleasedEvent`, invocación del caso de uso, publicación de `ticket.returned_to_inventory` cuando resultado es `NoEligible` o `AllFailed`, ACK en éxito/fallo de negocio, NACK sin requeue en fallo técnico — en `crud_service/src/CrudService.Infrastructure/Messaging/TicketReleasedConsumer.cs`
- [X] T021 Registrar `TicketReleasedConsumer` como `HostedService` en `crud_service/src/CrudService.Api/Program.cs`

**Checkpoint**: Infraestructura completa — consumer, adaptadores y puertos listos. Puede comenzar la implementación de user stories.

---

## Phase 3: User Story 1 — Asignación exitosa al siguiente comprador elegible (Priority: P1) 🎯 MVP

**Goal**: Cuando una entrada se libera y hay compradores elegibles, el sistema asigna una oportunidad al más antiguo, reserva la entrada temporalmente, transiciona la inscripción a consumed, y publica evento.

**Independent Test**: Publicar `ticket.released` con inscripción activa; verificar oportunidad active, inscripción consumed, y evento publicado.

### Tests para User Story 1 (TDD RED)

> **Escribir estos tests PRIMERO. Deben FALLAR antes de implementar.**

- [X] T022 [US1] Escribir test `AssignOpportunity_EligibleBuyerAndReservationSuccess_CreatesActiveOpportunity` (TC-HU3-01): mock strategy retorna entry, mock reservation retorna true, verificar que handler crea oportunidad Pending, transiciona a Active con TransitionTo, establece `ExpiresAt` calculado desde `ActivatedAt` (no desde la creación como Pending), llama UpdateStatusAsync(consumed), invoca observer — en `crud_service/tests/CrudService.Application.Tests/Waitlist/AssignOpportunityHandlerTests.cs`
- [X] T023 [US1] Escribir test `AssignOpportunity_MultipleBuyers_AssignsToOldest` (TC-HU3-04): mock repo retorna lista con 3 entries, mock strategy selecciona la de EnrolledAt más antiguo de la lista recibida, verificar que handler asigna al primero devuelto por strategy — en `crud_service/tests/CrudService.Application.Tests/Waitlist/AssignOpportunityHandlerTests.cs`

### Implementation para User Story 1 (TDD GREEN)

- [X] T024 [US1] Implementar `AssignOpportunityHandler` con flujo completo: (0) verificar si la lista de espera cerró (fecha del evento alcanzada) — si cerró, retornar NoEligible con log "waitlist closed" (FR-013), (1) verificar idempotencia (FindActiveByTicketIdAsync), (2) cargar lista completa de elegibles via `IWaitlistEntryRepository.GetActiveEntriesByEventAsync(eventId)`, (3) iterar la lista con `foreach`: para cada entry, llamar `IPrioritizationStrategy.SelectNextEligible(remainingEntries)`, crear oportunidad Pending, intentar reserva, (4) si éxito: TransitionTo(Active), set expiresAt/activatedAt, UpdateStatusAsync(consumed), notificar observer, retornar Assigned, (5) si fallo: TransitionTo(Failed), log, remover entry de la lista y continuar, (6) si la lista se agota: retornar AllFailed — en `crud_service/src/CrudService.Application/UseCases/Waitlist/AssignOpportunity/AssignOpportunityHandler.cs`

**Checkpoint**: US1 completa — asignación FIFO funcional con transiciones Pending→Active y notificación Observer.

---

## Phase 4: User Story 2 — Devolución al inventario sin compradores elegibles (Priority: P2)

**Goal**: Cuando no hay inscripciones activas, el handler retorna NoEligible y el consumer publica `ticket.returned_to_inventory`.

**Independent Test**: Publicar `ticket.released` sin inscripciones activas; verificar que se publica `ticket.returned_to_inventory` y no se crea oportunidad.

### Tests para User Story 2 (TDD RED)

- [X] T025 [US2] Escribir test `AssignOpportunity_NoEligibleBuyers_ReturnsNoEligible` (TC-HU3-02): mock repo retorna lista vacía, mock strategy retorna null, verificar que handler retorna resultado NoEligible, no crea oportunidad, no invoca observer — en `crud_service/tests/CrudService.Application.Tests/Waitlist/AssignOpportunityHandlerTests.cs`

### Implementation para User Story 2 (TDD GREEN)

> La lógica ya está en el handler (T024): cuando strategy retorna null en la primera iteración, retorna NoEligible. El consumer (T020) ya publica `ticket.returned_to_inventory` para este resultado. Si el test T025 pasa sin cambios adicionales, marcar como GREEN directamente.

**Checkpoint**: US2 completa — las entradas sin demanda vuelven al inventario automáticamente.

---

## Phase 5: User Story 3 — Manejo de fallo en la reserva temporal (Priority: P2)

**Goal**: Cuando la reserva falla, la oportunidad transiciona a Failed, la inscripción permanece activa, y el sistema itera al siguiente elegible. Si todos fallan, devuelve al inventario.

**Independent Test**: Forzar fallo en reserva; verificar oportunidad Failed, inscripción activa, log de diagnóstico.

### Tests para User Story 3 (TDD RED)

- [X] T026 [US3] Escribir test `AssignOpportunity_ReservationFails_TransitionsToFailedAndTriesNext` (TC-HU3-03): mock repo retorna lista con entry1 y entry2, mock strategy retorna entry1 en primera llamada (con lista completa) y entry2 en segunda llamada (con lista sin entry1), mock reservation retorna false para entry1 y true para entry2, verificar que primera oportunidad se creó como Pending y transicionó a Failed (existe con status Failed para trazabilidad), segunda oportunidad queda Active, entry1 permanece active, entry2 pasa a consumed — en `crud_service/tests/CrudService.Application.Tests/Waitlist/AssignOpportunityHandlerTests.cs`

> **Nota (F6)**: TC-HU3-03 en TestCases.md dice "No se crea ninguna oportunidad", pero la spec (FR-003) y hu3-plan.md definen que la oportunidad SÍ se crea como Pending antes de intentar la reserva (para trazabilidad) y transiciona a Failed si falla. La versión correcta es la de la spec: la oportunidad existe con status Failed. Documentar esta inconsistencia con TestCases.md.
- [X] T027 [US3] Escribir test `AssignOpportunity_AllReservationsFail_ReturnsAllFailed`: mock strategy retorna entry1 luego null, mock reservation retorna false, verificar que resultado es AllFailed, oportunidad se creó como Pending y transicionó a Failed (existe con status Failed), inscripción permanece active — en `crud_service/tests/CrudService.Application.Tests/Waitlist/AssignOpportunityHandlerTests.cs`

### Implementation para User Story 3 (TDD GREEN)

> La lógica de iteración y TransitionTo(Failed) ya está en el handler (T024). Si los tests T026 y T027 pasan sin cambios adicionales, marcar como GREEN directamente. Si fallan, ajustar el handler.

**Checkpoint**: US3 completa — fallos de reserva manejados con iteración FIFO y diagnóstico.

---

## Phase 6: User Story 4 — Protección contra asignación duplicada simultánea (Priority: P3)

**Goal**: El sistema no asigna dos oportunidades activas al mismo comprador. La restricción única de BD previene duplicados.

**Independent Test**: Verificar que la idempotencia (FindActiveByTicketIdAsync) ignora mensajes duplicados.

### Tests para User Story 4 (TDD RED)

- [X] T028 [US4] Escribir test `AssignOpportunity_ActiveOpportunityAlreadyExistsForTicket_ReturnsAssignedWithoutCreating`: mock FindActiveByTicketIdAsync retorna oportunidad existente, verificar que handler retorna inmediatamente sin llamar a strategy ni crear nueva oportunidad — en `crud_service/tests/CrudService.Application.Tests/Waitlist/AssignOpportunityHandlerTests.cs`

### Implementation para User Story 4 (TDD GREEN)

> La verificación de idempotencia ya está en el handler (T024, paso 1). Si el test T028 pasa sin cambios adicionales, marcar como GREEN directamente. Si falla, ajustar el handler.

- [X] T029 [US4] Agregar DOS índices parciales únicos en `scripts/schema.sql`: (1) `waitlist_opportunities(ticket_id) WHERE status = 'active'` (idempotencia) y (2) `waitlist_opportunities(waitlist_entry_id) WHERE status = 'active'` (concurrencia per-inscription) — deben coincidir con los declarados en T018

**Checkpoint**: US4 completa — protección contra duplicados a nivel de aplicación y BD.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Topología RabbitMQ, compilación, ejecución de tests y validación quickstart.

- [X] T030 Agregar declaraciones de colas y bindings (`q.ticket.released`, `q.ticket.returned`, `q.waitlist.opportunity.delay` con TTL y DLX, `q.waitlist.opportunity.expired`) en `scripts/setup-rabbitmq.sh`
- [X] T031 [P] Verificar compilación de toda la solución con `dotnet build crud_service/CrudService.sln`
- [X] T032 [P] Ejecutar todos los tests con `dotnet test crud_service/CrudService.sln --filter "Waitlist"` y confirmar que pasan
- [X] T033 Ejecutar validación manual de quickstart.md: publicar mensaje `ticket.released` con `rabbitmqadmin` y verificar flujo completo

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sin dependencias — puede empezar inmediatamente
- **Foundational (Phase 2)**: Depende de Phase 1 (los adaptadores necesitan los puertos definidos) — BLOQUEA todas las user stories
- **User Story 1 (Phase 3)**: Depende de Phase 2 — el handler necesita todos los puertos y adaptadores
- **User Story 2 (Phase 4)**: Depende de Phase 3 (el handler base debe existir para verificar el branch NoEligible)
- **User Story 3 (Phase 5)**: Depende de Phase 3 (el handler base debe existir para verificar el branch de iteración)
- **User Story 4 (Phase 6)**: Depende de Phase 3 (el handler base debe existir para verificar la idempotencia)
- **Polish (Phase 7)**: Depende de todas las user stories

### Parallel Execution Within Phases

**Phase 1**: T002-T005 y T008-T012 son [P] — pueden ejecutarse en paralelo (archivos distintos)  
**Phase 2**: T014-T015 son [P] — adaptadores independientes  
**Phase 4-6**: Pueden ejecutarse en paralelo entre sí (todas dependen de Phase 3, no entre ellas)  
**Phase 7**: T031-T032 son [P] — build y test independientes

### Suggested MVP Scope

**MVP = Phase 1 + Phase 2 + Phase 3 (US1)**: Con solo US1 implementada, el sistema puede asignar oportunidades FIFO cuando hay elegibles. US2-US4 agregan robustez pero US1 es funcional de forma independiente.

### Implementation Strategy

1. **Setup**: crear todos los puertos e interfaces primero (define contratos)
2. **Foundation**: implementar adaptadores e infraestructura (cumple contratos)
3. **US1**: tests RED del handler, luego implementación GREEN (flujo principal)
4. **US2-US4**: tests RED de branches secundarios — la mayoría ya cubiertos por el handler de US1; solo verificar y ajustar si necesario
5. **Polish**: topología RabbitMQ, build final, validación manual
