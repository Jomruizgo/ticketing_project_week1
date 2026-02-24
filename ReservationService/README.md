# ReservationService - Consumer Service 1

Microservicio responsable de procesar las reservas de tickets consumiendo mensajes de RabbitMQ.

## Responsabilidad

- Consumir mensajes de la cola `q.ticket.reserved`
- Consumir mensajes de la cola `q.ticket.expired`
- Validar disponibilidad del ticket (debe estar en estado `available`)
- Actualizar estado del ticket a `reserved` en PostgreSQL con optimistic locking
- Registrar `reserved_at` y calcular `expires_at` (reserva + duracion en segundos)
- Rechazar reservas si el ticket ya no esta disponible o hay conflicto de concurrencia
- Liberar tickets expirados con transicion segura `reserved -> released`

## Actualización Feature A (Dev A) — 2026-02-24

Implementación completada en `feature/expiry-core-reservation`:

- Nuevo caso de uso de expiración:
  - `ReservationService.Application/Interfaces/IProcessExpirationUseCase.cs`
  - `ReservationService.Application/UseCases/ProcessExpiration/ProcessExpirationCommandHandler.cs`
- Nuevo consumer de expiración:
  - `ReservationService.Infrastructure/Messaging/TicketExpiredConsumer.cs`
  - Escucha `q.ticket.expired` y publica `ticket.status.changed` con `released` cuando aplica.
- Repositorio extendido con liberación segura:
  - `ReservationService.Domain/Interfaces/ITicketRepository.cs` agrega `TryReleaseAsync(...)`
  - `ReservationService.Infrastructure/Persistence/Repositories/TicketRepository.cs` implementa `TryReleaseAsync(...)` con optimistic locking (`id + version + status=reserved`).
- Configuración:
  - `ReservationService.Infrastructure/Messaging/RabbitMQSettings.cs` agrega `ExpiredQueueName`.
  - `ReservationService.Worker/appsettings.json` incorpora `RabbitMQ:ExpiredQueueName`.

### Verificación realizada en entorno real (Docker)

Se validó el flujo integrado levantando contenedores (`docker compose up -d --build`):

1. Crear evento y ticket en `crud_service`.
2. Reservar ticket vía `producer` (`/api/tickets/reserve`).
3. Publicar evento `ticket.expired` en exchange `tickets` (RabbitMQ Management API).
4. Verificar transición a `Released` vía `crud_service` (`/api/tickets/{id}`).
5. Confirmar logs en `reservation-service`:
   - `Expiration message received`
   - `Ticket <id> released successfully`
   - `Ticket <id> released due to expiration`

### Dependencia pendiente de Dev B

Queda fuera de este alcance la validación final del relay SSE/read-model en `crud_service` (Dev B).

## Stack

- .NET 8 (LTS)
- RabbitMQ.Client 6.8.1
- Entity Framework Core 8.0 + Npgsql (PostgreSQL)
- xUnit + NSubstitute (tests)

## Estructura del Proyecto

```
ReservationService/
├── ReservationService.sln
├── Dockerfile
├── README.md
├── src/
│   └── ReservationService.Worker/
│       ├── Consumers/
│       │   └── TicketReservationConsumer.cs   # Consumer RabbitMQ (BackgroundService)
│       ├── Models/
│       │   ├── ReservationMessage.cs          # DTO del mensaje de RabbitMQ
│       │   ├── Ticket.cs                      # Entidad Ticket
│       │   └── TicketStatus.cs                # Enum de estados
│       ├── Services/
│       │   ├── IReservationService.cs         # Interfaz de logica de negocio
│       │   └── ReservationService.cs          # Implementacion: validacion + reserva
│       ├── Repositories/
│       │   ├── ITicketRepository.cs           # Interfaz de acceso a datos
│       │   └── TicketRepository.cs            # UPDATE con optimistic locking
│       ├── Data/
│       │   └── TicketingDbContext.cs           # DbContext con ValueConverter para enums
│       ├── Configurations/
│       │   └── RabbitMQSettings.cs            # Opciones de configuracion RabbitMQ
│       ├── Program.cs
│       └── appsettings.json
└── tests/
    └── ReservationService.Worker.Tests/
        ├── ReservationServiceImplTests.cs     # 4 unit tests de logica de negocio
        └── ReservationService.Worker.Tests.csproj
```

## Configuracion

### Variables de Entorno

| Variable | Descripcion | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | Connection string PostgreSQL | - |
| `RabbitMQ__Host` | Host de RabbitMQ | localhost |
| `RabbitMQ__Port` | Puerto AMQP | 5672 |
| `RabbitMQ__Username` | Usuario RabbitMQ | guest |
| `RabbitMQ__Password` | Password RabbitMQ | guest |

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ticketing_db;Username=ticketing_user;Password=ticketing_password"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "QueueName": "q.ticket.reserved"
  }
}
```

## Ejecucion Local

```bash
# Desde la raiz del microservicio
cd ReservationService

# Restaurar dependencias
dotnet restore

# Ejecutar
dotnet run --project src/ReservationService.Worker
```

## Ejecucion con Docker

```bash
# Desde la raiz del repositorio, levanta todos los servicios
docker compose up -d

# O solo el reservation service
docker compose up -d reservation-service
```

## Tests

```bash
cd ReservationService

# Ejecutar tests
dotnet test
```

### Casos de prueba

| Test | Escenario | Resultado esperado |
|------|-----------|-------------------|
| `ProcessReservation_TicketNotFound_ReturnsFailure` | Ticket no existe en BD | Retorna failure, no modifica nada |
| `ProcessReservation_TicketAlreadyReserved_ReturnsFailure` | Ticket ya esta reservado | Retorna failure por estado invalido |
| `ProcessReservation_AvailableTicket_ReturnsSuccess` | Ticket disponible, sin conflictos | Reserva exitosa, actualiza estado y timestamps |
| `ProcessReservation_ConcurrentModification_ReturnsFailure` | Dos reservas simultaneas al mismo ticket | Una falla por optimistic locking (version mismatch) |

## Flujo de Procesamiento

```
[RabbitMQ: q.ticket.reserved]
         |
         v
+--------------------------+
|  TicketReservationConsumer|    autoAck: false
|  (BackgroundService)      |    prefetchCount: 1
+-----------+--------------+
            |
            v
+--------------------------+
|  ReservationServiceImpl   |
|  - Validar ticket existe  |
|  - Verificar status =     |
|    available              |
+-----------+--------------+
            |
            v
+--------------------------+
|  TicketRepository         |
|  - ExecuteUpdateAsync     |
|  - WHERE version = N      |    <-- optimistic locking
|  - SET status = reserved  |
|  - SET version = N + 1    |
+-----------+--------------+
            |
            v
     [PostgreSQL]
            |
            v
  ACK si exito / NACK si error tecnico
```

## Criterios de Aceptacion

- [x] Si el ticket ya no esta DISPONIBLE, el mensaje debe descartarse
- [x] Usar optimistic locking (campo `version`) para evitar race conditions
- [x] Registrar `reserved_at` y calcular `expires_at`
- [x] Loguear errores cuando no se pueda procesar una reserva

## Lo que la IA hizo mal

| Fecha | Descripcion | Correccion |
|-------|-------------|------------|
| 2026-02-11 | La IA genero el modelo `Ticket` con una propiedad `SectionId` que no existia en el schema de PostgreSQL. Al ejecutar, EF Core intentaba mapear una columna `section_id` inexistente, causando error en runtime. La IA no verifico el schema real antes de generar el modelo. | Se elimino `SectionId` del modelo y su mapping del DbContext. Leccion: siempre contrastar el modelo generado con `scripts/schema.sql`. |
| 2026-02-11 | La IA uso `.HasConversion<string>()` para mapear el enum `TicketStatus` a PostgreSQL. Pero la BD usa enums nativos en lowercase (`available`, `reserved`) y el enum de C# era PascalCase (`Available`, `Reserved`). Cada INSERT/SELECT fallaba con error de tipo. | Se reemplazo por un `ValueConverter` explicito: `v => v.ToString().ToLower()` para escritura y `Enum.Parse<TicketStatus>(v, true)` para lectura. Leccion: no confiar en conversiones por defecto cuando la BD tiene tipos especificos. |
| 2026-02-11 | La IA proponia funcionalidades validas pero fuera del alcance del MVP por cuestion de tiempo: idempotencia en el consumer, health checks dedicados, tests de integracion y de infraestructura ademas de los unit tests. Son practicas correctas en un proyecto real, pero no eran prioridad para la entrega. | Se delimito el alcance: solo unit tests de logica de negocio (4 tests en `ReservationServiceImpl`), sin idempotencia ni health checks propios. Leccion: la IA no gestiona prioridades ni deadlines; el humano debe decidir que se implementa ahora y que queda para despues. |

---

**Responsable:** Jorge
