# MsPaymentService.Worker

Microservicio **Worker** del dominio de pagos del sistema de ticketing. Consume eventos de pago (aprobado/rechazado) desde RabbitMQ, valida reglas de negocio y actualiza el estado de tickets y pagos en PostgreSQL.

---

## Índice

- [Descripción general](#descripción-general)
- [Requisitos](#requisitos)
- [Arquitectura](#arquitectura)
- [Flujo de mensajes](#flujo-de-mensajes)
- [Modelos de datos](#modelos-de-datos)
- [Configuración](#configuración)
- [Ejecución](#ejecución)
- [Estructura del proyecto](#estructura-del-proyecto)

---

## Descripción general

El Worker es un **Background Service** (.NET 8) que:

1. **Escucha** colas de RabbitMQ (definidas en `scripts/setup-rabbitmq.sh`):
   - `q.ticket.payments.requested` — solicitud de procesamiento de pago.
   - `q.ticket.payments.approved` — pagos aprobados por el proveedor.
   - `q.ticket.payments.rejected` — pagos rechazados.

2. **Valida** cada evento (idempotencia, estado del ticket, TTL de reserva, estado del pago).

3. **Procesa** las transiciones de estado:
   - **Pago aprobado:** ticket `reserved` → `paid`, payment `pending` → `approved`.
   - **Pago rechazado / TTL excedido:** ticket → `released`, payment → `failed` o `expired`.

4. **Registra** el historial de cambios de estado en `TicketHistory` para auditoría.

Todo se hace dentro de **transacciones** con bloqueo pesimista donde aplica, y con **control de concurrencia optimista** en entidades `Ticket`/`Payment`.

---

## Requisitos

- **.NET 8**
- **PostgreSQL** (con schema y enums usados por el Worker)
- **RabbitMQ** (exchange `ticket.payments` y colas configuradas)

---

## Arquitectura

```
RabbitMQ (tickets exchange)
 ├─ q.ticket.payments.requested
 ├─ q.ticket.payments.approved
 └─ q.ticket.payments.rejected
                  │
                  ▼
TicketPaymentConsumer
                  │
                  ▼
IPaymentEventDispatcher
 ├─ PaymentRequestedEventHandler
 ├─ PaymentApprovedEventHandler → ProcessApprovedPaymentCommandHandler
 └─ PaymentRejectedEventHandler → ProcessRejectedPaymentCommandHandler
                                                       │
                                                       ▼
                                           TicketStateService
                                                       │
                                                       ▼
                     Repositories → PaymentDbContext (PostgreSQL)
                                                       │
                                                       ▼
                                  StatusChangedPublisher (ticket.status.changed)
```

- **Worker:** mantiene el proceso vivo y arranca los consumers.
- **TicketPaymentConsumer:** deserializa mensajes, delega en `IPaymentEventDispatcher` y hace ACK/NACK según resultado.
- **Handlers + CommandHandlers:** aplican reglas de negocio (idempotencia, TTL, estados) por tipo de evento.
- **TicketStateService:** transacciones de base de datos y cambios de estado (paid/released) + historial.

---

## Flujo de mensajes

### Evento: Pago aprobado (`PaymentApprovedEvent`)

1. Llega mensaje a la cola `q.ticket.payments.approved`.
2. Se valida:
   - Ticket existe.
   - Idempotencia: si el ticket ya está `paid`, se considera ya procesado (ACK sin cambios).
   - Estado actual del ticket debe ser `reserved`.
   - TTL: `PublishedAt` del evento debe ser ≤ `ReservedAt + 5 min`; si no, se marca ticket como `released` y se responde fallo por TTL.
   - Payment existe y está en estado `pending`.
3. Si todo es válido: transición a `paid` (ticket + payment) y registro en historial. ACK.
4. Si fallo de negocio: ACK (no requeue). Si error técnico: NACK sin requeue (mensaje puede ir a DLQ si está configurada).

### Evento: Pago rechazado (`PaymentRejectedEvent`)

1. Llega mensaje a la cola `q.ticket.payments.rejected`.
2. Se valida:
   - Ticket existe.
   - Idempotencia: si ya está `released`, se considera ya procesado (ACK).
3. Transición a `released` y actualización del payment a `failed`. Registro en historial. ACK.

### Contrato de colas (resumen)

| Cola                          | Routing Key                 | Evento                |
|------------------------------|-----------------------------|------------------------|
| `q.ticket.payments.approved` | `ticket.payments.approved`  | `PaymentApprovedEvent` |
| `q.ticket.payments.rejected` | `ticket.payments.rejected`  | `PaymentRejectedEvent` |

---

## Modelos de datos

### Entidades principales

- **Ticket:** `Id`, `EventId`, `Status` (available | reserved | paid | released | cancelled), `ReservedAt`, `ExpiresAt`, `PaidAt`, `OrderId`, `ReservedBy`, `Version`.
- **Payment:** `Id`, `TicketId`, `Status` (pending | approved | failed | expired), `ProviderRef`, `AmountCents`, `Currency`, `CreatedAt`, `UpdatedAt`.
- **TicketHistory:** auditoría de cambios de estado (`TicketId`, `OldStatus`, `NewStatus`, `ChangedAt`, `Reason`).
- **Event:** `Id`, `Name`, `StartsAt` (entidad de catálogo asociada a tickets).

Los enums `TicketStatus` y `PaymentStatus` se persisten como tipos enum de PostgreSQL (`ticket_status`, `payment_status`).

### Eventos de mensajería

- **PaymentApprovedEvent:** `TicketId`, `EventId`, `OrderId`, `ReservedBy`, `ReservationDurationSeconds`, `PublishedAt`.
- **PaymentRejectedEvent:** `TicketId`, `PaymentId`, `ProviderReference`, `RejectionReason`, `RejectedAt`, `EventId`, `EventTimestamp`.

---

## Configuración

Archivo principal: `src/MsPaymentService.Worker/appsettings.json`.

### ConnectionStrings

| Clave        | Descripción                    |
|-------------|---------------------------------|
| `TicketingDb` | Cadena de conexión a PostgreSQL |

Ejemplo:  
`Host=localhost;Database=ticketing;Username=ticketing_user;Password=ticketing_password`

### RabbitMQ

La **topología** (exchange `tickets`, colas, bindings) se define y crea en **`scripts/`**: `setup-rabbitmq.sh` y `rabbitmq-definitions.json`. Este Worker **consume** eventos de pago y también **publica** `ticket.status.changed` cuando una transición de estado se confirma. La config incluye conexión, exchange y nombres de colas a escuchar (deben coincidir con los del script).

| Clave                 | Descripción                                      |
|-----------------------|--------------------------------------------------|
| `HostName`, `Port`    | Servidor y puerto RabbitMQ                       |
| `UserName`, `Password`| Credenciales                                     |
| `VirtualHost`         | Virtual host (por defecto `/`)                   |
| `ExchangeName`        | Exchange usado para publish/consume (default: `tickets`) |
| `RequestedQueueName`  | Cola de solicitud de pago (default en entorno)   |
| `ApprovedQueueName`   | Cola de pagos aprobados (default: `q.ticket.payments.approved`) |
| `RejectedQueueName`   | Cola de pagos rechazados (default: `q.ticket.payments.rejected`) |
| `PrefetchCount`       | Mensajes sin ACK en vuelo por canal (default: 10) |

### PaymentSettings

| Clave                  | Descripción                          | Valor por defecto |
|------------------------|--------------------------------------|-------------------|
| `ReservationTtlMinutes`| TTL de reserva para validar pago      | 5                 |
| `MaxRetryAttempts`     | Reintentos (referencia)               | 3                 |
| `RetryDelaySeconds`    | Delay entre reintentos (referencia)  | 5                 |

El TTL real usado en validación se toma de configuración (`ReservationTtlMinutes`) a través de `IPaymentConfiguration`.

---

## Ejecución

Desde la raíz del repositorio:

```bash
cd paymentService/src/MsPaymentService.Worker
dotnet run
```

O desde la raíz de la solución:

```bash
dotnet run --project paymentService/src/MsPaymentService.Worker
```

Variables de entorno útiles:

- `ASPNETCORE_ENVIRONMENT=Development` — habilita logging sensible y errores detallados de EF.
- Connection string y RabbitMQ pueden sobreescribirse por variables de entorno o User Secrets (el proyecto tiene `UserSecretsId` configurado).

---

## Estructura del proyecto

```
paymentService/
├── src/
│   ├── MsPaymentService.Domain/          # Entidades + contratos de dominio
│   ├── MsPaymentService.Application/     # Casos de uso, DTOs, puertos
│   ├── MsPaymentService.Infrastructure/  # EF Core + RabbitMQ (adaptadores)
│   └── MsPaymentService.Worker/          # Composition root + BackgroundService
└── tests/
   └── MsPaymentService.Application.Tests/
```

---

## Dependencias principales

- **Microsoft.EntityFrameworkCore** 8.0.4  
- **Npgsql.EntityFrameworkCore.PostgreSQL** 8.0.4  
- **RabbitMQ.Client** 6.8.1  
- **Microsoft.Extensions.Hosting** 8.0.1  

---

## Notas

- **Idempotencia:** eventos duplicados (mismo ticket ya pagado o ya liberado) se detectan y se hace ACK sin reprocesar.
- **TTL:** si el pago aprobado llega después del TTL de la reserva, el ticket se libera y el pago no se confirma.
- **Transacciones:** las transiciones de estado se ejecutan en transacciones de base de datos con rollback en caso de error.
- **Concurrencia:** se usa `Version` en entidades y, donde aplica, `GetByIdForUpdateAsync` para bloqueo pesimista.
