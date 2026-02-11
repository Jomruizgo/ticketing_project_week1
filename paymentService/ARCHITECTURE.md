# Arquitectura — MsPaymentService.Worker

Documento de arquitectura del microservicio de procesamiento de pagos (Worker). Describe el contexto, los componentes, el flujo de datos y las decisiones de diseño.

---

## Índice

1. [Contexto y límites](#1-contexto-y-límites)
2. [Vista de alto nivel](#2-vista-de-alto-nivel)
3. [Componentes y responsabilidades](#3-componentes-y-responsabilidades)
4. [Flujo de datos y eventos](#4-flujo-de-datos-y-eventos)
5. [Persistencia y concurrencia](#5-persistencia-y-concurrencia)
6. [Mensajería y contratos](#6-mensajería-y-contratos)
7. [Decisiones de diseño](#7-decisiones-de-diseño)
8. [Diagramas de secuencia](#8-diagramas-de-secuencia)

---

## 1. Contexto y límites

### 1.1 Rol en el sistema

El **MsPaymentService.Worker** pertenece al **dominio de pagos** del sistema de ticketing. No expone APIs HTTP; su única interfaz con el exterior es **consumir mensajes** desde un broker (RabbitMQ).

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        SISTEMA DE TICKETING                              │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────────────────┐   │
│  │ Reservas /   │    │ Proveedor    │    │ MsPaymentService.Worker   │   │
│  │ Tickets      │───▶│ de pagos     │───▶│ (este servicio)           │   │
│  └──────────────┘    └──────────────┘    └──────────────────────────┘   │
│         │                     │                         │                │
│         │                     │                         │                │
│         └─────────────────────┴─────────────────────────┘                │
│                               │                                          │
│                         RabbitMQ + PostgreSQL                            │
└─────────────────────────────────────────────────────────────────────────┘
```

- **Entradas:** eventos `ticket.payments.approved` y `ticket.payments.rejected` publicados por otros servicios (p. ej. un adaptador del proveedor de pagos).
- **Salidas:** cambios en la base de datos (estado de tickets, pagos e historial). No publica eventos hacia el broker en la implementación actual.
- **Límite de contexto:** el Worker **no** orquesta reservas ni llama a pasarelas de pago; solo **reacciona** al resultado del pago y actualiza el estado del dominio (ticket/payment).

### 1.2 Bounded Context

Dentro del bounded context **Pagos**:

- **Responsabilidad:** interpretar el resultado del pago (aprobado/rechazado) y aplicar las transiciones de estado permitidas sobre tickets y pagos.
- **No es responsable de:** crear reservas, calcular precios, integrar con el proveedor de pago ni notificar al usuario (eso corresponde a otros contextos/servicios).

---

## 2. Vista de alto nivel

### 2.1 Capas

```
┌─────────────────────────────────────────────────────────────────┐
│  Host / Runtime                                                  │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │  Worker (BackgroundService)                                  ││
│  │  - Mantiene el proceso activo                                ││
│  │  - Inicia consumers por cola                                 ││
│  └─────────────────────────────────────────────────────────────┘│
├─────────────────────────────────────────────────────────────────┤
│  Messaging (Infraestructura)                                     │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │  RabbitMQConnection  │  TicketPaymentConsumer                 ││
│  │  - Conexión/canal   │  - Deserialización, dispatch, ACK/NACK ││
│  └─────────────────────────────────────────────────────────────┘│
├─────────────────────────────────────────────────────────────────┤
│  Application (Orquestación)                                       │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │  PaymentValidationService                                    ││
│  │  - Validación de reglas de negocio (idempotencia, TTL, etc.)││
│  │  - Coordina con TicketStateService                           ││
│  └─────────────────────────────────────────────────────────────┘│
├─────────────────────────────────────────────────────────────────┤
│  Domain (Lógica de estado)                                        │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │  TicketStateService                                          ││
│  │  - Transiciones paid / released                              ││
│  │  - Transacciones y escritura en historial                    ││
│  └─────────────────────────────────────────────────────────────┘│
├─────────────────────────────────────────────────────────────────┤
│  Data Access                                                      │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │  Repositories (Ticket, Payment, TicketHistory)               ││
│  │  PaymentDbContext — PostgreSQL                               ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 Dependencias entre componentes

- **Worker** → **TicketPaymentConsumer** (arranque de colas).
- **TicketPaymentConsumer** → **IPaymentValidationService** (por mensaje, vía scope).
- **PaymentValidationService** → **ITicketRepository**, **IPaymentRepository**, **ITicketStateService**.
- **TicketStateService** → **PaymentDbContext**, **ITicketRepository**, **IPaymentRepository**, **ITicketHistoryRepository**.

La infraestructura (RabbitMQ, DbContext) se inyecta en los puntos anteriores; no hay dependencias circulares.

---

## 3. Componentes y responsabilidades

### 3.1 Worker

- **Tipo:** `BackgroundService`.
- **Responsabilidad:** mantener el host en ejecución y registrar los consumers en las colas `ticket.payments.approved` y `ticket.payments.rejected`.
- **Patrón:** host de proceso largo; el consumo real lo hace `TicketPaymentConsumer`.

### 3.2 Messaging

| Componente           | Responsabilidad |
|----------------------|-----------------|
| **RabbitMQConnection** | Singleton que gestiona `IConnection` e `IModel` con reconexión automática (`AutomaticRecoveryEnabled`, `DispatchConsumersAsync`). |
| **TicketPaymentConsumer** | Suscripción a las colas, deserialización JSON según `RoutingKey`, resolución de `IPaymentValidationService` por scope, invocación de validación y envío de ACK/NACK según resultado. |

El consumer no contiene lógica de negocio; solo adapta mensajes a la aplicación y traduce el resultado de validación en semántica de cola (ACK = procesado, NACK sin requeue = error técnico / DLQ).

### 3.3 PaymentValidationService

- **Responsabilidad:** aplicar las reglas de negocio antes de cambiar estado.
- **Operaciones:**
  - **ValidateAndProcessApprovedPaymentAsync:** comprueba existencia del ticket, idempotencia (ya `paid`), estado `reserved`, TTL de la reserva y estado del payment `pending`; si todo es válido, llama a `TransitionToPaidAsync`; si el TTL se excedió, llama a `TransitionToReleasedAsync` y devuelve fallo por TTL.
  - **ValidateAndProcessRejectedPaymentAsync:** comprueba existencia, idempotencia (ya `released`) y llama a `TransitionToReleasedAsync`.
- **TTL:** el límite temporal (p. ej. 5 minutos desde `ReservedAt`) se aplica aquí; el Worker no extiende la reserva.

### 3.4 TicketStateService

- **Responsabilidad:** ejecutar las transiciones de estado en base de datos de forma atómica y auditable.
- **Transiciones:**
  - **TransitionToPaidAsync:** bloqueo del ticket (`GetByIdForUpdateAsync`), actualización a `paid` y `PaidAt`, actualización del payment a `approved` con `ProviderRef`, registro en `TicketHistory`, commit.
  - **TransitionToReleasedAsync:** bloqueo del ticket, actualización a `released`, actualización del payment a `failed` o `expired` según motivo, registro en historial, commit.
- **Concurrencia:** usa transacciones explícitas y repositorios que soportan `FOR UPDATE` y actualización con versión.

### 3.5 Repositories y DbContext

- **PaymentDbContext:** define `Tickets`, `Payments`, `TicketHistory`, `Events` y mapea enums de PostgreSQL (`ticket_status`, `payment_status`).
- **Repositorios:** abstraen consultas y comandos. El contrato `ITicketRepository` incluye `GetByIdForUpdateAsync` para bloqueo pesimista y `UpdateAsync` para concurrencia optimista con `Version`.

---

## 4. Flujo de datos y eventos

### 4.1 Dirección del flujo

El flujo es **unidireccional:** mensaje → validación → actualización de estado → persistencia. No hay publicación de eventos salientes en este Worker.

### 4.2 Estados del ticket y del pago

```
Ticket:   available → reserved → paid
               ↑           │
               └───────────┴──→ released
                              (por rechazo, TTL o cancelación)

Payment:  pending → approved
              │
              └──→ failed | expired  (rechazo o TTL)
```

Las transiciones que aplica este Worker son:

- **Aprobado válido:** `reserved` → `paid`, `pending` → `approved`.
- **Rechazado o TTL excedido:** ticket → `released`, payment → `failed` o `expired`.

### 4.3 Idempotencia

- **Pago aprobado:** si el ticket ya está en `paid`, se considera duplicado y se responde “ya procesado” sin modificar datos; el mensaje se hace ACK.
- **Pago rechazado:** si el ticket ya está en `released`, mismo criterio.
- Con esto se evita doble aplicación del mismo resultado de pago ante reintentos o mensajes duplicados.

---

## 5. Persistencia y concurrencia

### 5.1 Modelo de datos (resumen)

- **Ticket:** clave de concurrencia optimista con `Version`; estados vía enum PostgreSQL.
- **Payment:** vinculado a `TicketId`; estados `pending`, `approved`, `failed`, `expired`.
- **TicketHistory:** append-only; registra cada cambio de estado (quién/cuándo/motivo no se modelan como entidad de usuario; el “motivo” va en `Reason`).
- **Event:** entidad de catálogo para el evento (concierto, etc.); los tickets referencian `EventId`.

### 5.2 Estrategia de concurrencia

- **Pesimista:** en las transiciones de estado se usa `GetByIdForUpdateAsync` (equivalente a `SELECT ... FOR UPDATE`) dentro de una transacción para evitar condiciones de carrera entre dos mensajes que procesen el mismo ticket.
- **Optimista:** en las entidades se usa el campo `Version`; las actualizaciones se hacen condicionadas a esa versión para detectar modificaciones concurrentes (y se puede propagar `DbUpdateConcurrencyException` para reintento o DLQ).

### 5.3 Transacciones

- `TransitionToPaidAsync` y `TransitionToReleasedAsync` abren una transacción de base de datos, ejecutan todas las escrituras (ticket, payment, historial) y hacen commit o rollback. Así se garantiza consistencia entre ticket, payment e historial.

---

## 6. Mensajería y contratos

### 6.1 Topología RabbitMQ (esperada)

- **Exchange:** `ticket.payments` (topic o direct).
- **Colas:**  
  - `ticket.payments.approved` (routing key `ticket.payments.approved`).  
  - `ticket.payments.rejected` (routing key `ticket.payments.rejected`).
- **Consumer:** `BasicQos(0, 1, false)` (prefetch 1 por canal) y `autoAck: false` para ACK/NACK manual.

### 6.2 Contratos de mensajes

- **PaymentApprovedEvent:**  
  `TicketId`, `EventId`, `OrderId`, `ReservedBy`, `ReservationDurationSeconds`, `PublishedAt` (UTC).
- **PaymentRejectedEvent:**  
  `TicketId`, `PaymentId`, `ProviderReference`, `RejectionReason`, `RejectedAt`, `EventId`, `EventTimestamp` (UTC).

La deserialización usa `PropertyNameCaseInsensitive = true`. Los campos deben ser compatibles con lo que publique el productor (tipos y nombres).

### 6.3 Semántica de entrega

- **ACK:** mensaje procesado correctamente o rechazado por reglas de negocio (no se reintenta).
- **NACK (requeue: false):** error técnico durante el procesamiento; el mensaje no se reencola (queda para DLQ si está configurada en el broker).
- No se hace requeue para no repetir indefinidamente mensajes que fallan por datos o reglas de negocio.

---

## 7. Decisiones de diseño

| Decisión | Justificación |
|----------|----------------|
| Worker sin API HTTP | Solo consume eventos; reduce superficie de ataque y despliegue. |
| Validación y estado en servicios separados | PaymentValidationService concentra reglas; TicketStateService concentra transacciones y escritura. Separación clara de responsabilidades. |
| Bloqueo pesimista en transiciones | Evita carreras entre dos eventos que intenten pasar el mismo ticket a `paid` o `released`. |
| Versión optimista en entidades | Detecta conflictos de escritura y permite reintentos o DLQ sin bloquear otras filas. |
| Historial append-only | Auditoría y trazabilidad sin modificar registros históricos. |
| ACK en fallo de negocio, NACK en fallo técnico | Evita bucles de reintento para errores de datos; errores técnicos pueden ir a DLQ. |
| Scope por mensaje para servicios | PaymentValidationService y dependencias son scoped; cada mensaje obtiene su propio DbContext y transacción. |
| Conexión RabbitMQ singleton, consumer singleton | Una conexión/canal compartido; el consumer mantiene la suscripción. Los handlers usan scope para acceso a BD. |

---

## 8. Diagramas de secuencia

### 8.1 Pago aprobado (éxito)

```
RabbitMQ          Consumer              PaymentValidationService    TicketStateService    Repositories    DB
   │                  │                            │                         │                  │       │
   │  message         │                            │                         │                  │       │
   │─────────────────▶│                            │                         │                  │       │
   │                  │ GetByIdAsync(ticket)       │                         │                  │       │
   │                  │───────────────────────────▶│─────────────────────────▶│─────────────────▶│──────▶│
   │                  │                            │                         │                  │       │
   │                  │ GetByTicketIdAsync(payment)│                         │                  │       │
   │                  │───────────────────────────▶│─────────────────────────▶│─────────────────▶│──────▶│
   │                  │                            │                         │                  │       │
   │                  │ TransitionToPaidAsync     │                         │                  │       │
   │                  │───────────────────────────▶│────────────────────────▶│                  │       │
   │                  │                            │                         │ BeginTransaction │       │
   │                  │                            │                         │─────────────────────────────────▶│
   │                  │                            │                         │ GetByIdForUpdate │       │
   │                  │                            │                         │─────────────────▶│──────▶│
   │                  │                            │                         │ Update ticket    │       │
   │                  │                            │                         │ Update payment   │       │
   │                  │                            │                         │ Add history      │       │
   │                  │                            │                         │ Commit           │       │
   │                  │                            │                         │─────────────────────────────────▶│
   │                  │◀───────────────────────────│◀────────────────────────│                  │       │
   │                  │ ValidationResult.Success   │                         │                  │       │
   │                  │ BasicAck                  │                         │                  │       │
   │◀─────────────────│                            │                         │                  │       │
```

### 8.2 Pago rechazado

```
RabbitMQ          Consumer              PaymentValidationService    TicketStateService    Repositories    DB
   │                  │                            │                         │                  │       │
   │  message         │                            │                         │                  │       │
   │─────────────────▶│                            │                         │                  │       │
   │                  │ ValidateAndProcessRejected │                         │                  │       │
   │                  │───────────────────────────▶│                         │                  │       │
   │                  │                            │ GetByIdAsync(ticket)     │                  │       │
   │                  │                            │─────────────────────────▶│─────────────────▶│──────▶│
   │                  │                            │ TransitionToReleasedAsync│                  │       │
   │                  │                            │────────────────────────▶│                  │       │
   │                  │                            │                         │ [transaction:     │       │
   │                  │                            │                         │  update ticket,  │       │
   │                  │                            │                         │  payment, history]│       │
   │                  │◀───────────────────────────│◀────────────────────────│                  │       │
   │                  │ BasicAck                  │                         │                  │       │
   │◀─────────────────│                            │                         │                  │       │
```

---

Este documento debe actualizarse cuando se añadan nuevos eventos, cambios en la topología de RabbitMQ o en el modelo de persistencia.
