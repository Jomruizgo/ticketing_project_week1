# ARQUITECTURA: CrudService

> Documento técnico de referencia para el equipo de desarrollo.
> Describe la arquitectura hexagonal implementada, las decisiones tomadas y cómo extender el servicio.

---

## 1. Contexto: de dónde venimos

### Antes — N-Layer (un solo proyecto)

```
CrudService.csproj
├── Controllers/        ← recibía HTTP
├── Services/           ← lógica de negocio
├── Repositories/       ← interfaces de datos
├── Data/               ← DbContext + implementaciones de repos
├── Models/             ← entidades y DTOs
└── Extensions/         ← registro de DI
```

**Problema:** todo el código vivía en un solo ensamblado. Un cambio en la base de datos podía romper la lógica de negocio. Los tests tenían que referenciar el proyecto completo (incluyendo EF Core, Npgsql, RabbitMQ), lo que hacía difícil aislar la lógica pura.

### Después — Arquitectura Hexagonal (4 proyectos)

```
CrudService.sln
├── src/
│   ├── CrudService.Domain/         ← el corazón: entidades e interfaces
│   ├── CrudService.Application/    ← casos de uso (lógica pura)
│   ├── CrudService.Infrastructure/ ← adaptadores: EF Core, RabbitMQ
│   └── CrudService.Api/            ← adaptador HTTP: controladores
└── tests/
    └── CrudService.Application.Tests/ ← tests sin dependencias de infraestructura
```

---

## 2. La regla de dependencias

Las dependencias solo van hacia adentro. Nunca al revés.

```
Api ──────┐
          ↓
Infrastructure ──→ Application ──→ Domain
```

- **Domain** no depende de nadie. No sabe que existe EF Core, ni RabbitMQ, ni HTTP.
- **Application** solo depende de Domain. No sabe que existe PostgreSQL.
- **Infrastructure** implementa los contratos definidos en Domain y orquesta la Application.
- **Api** orquesta Infrastructure y Application. Es la entrada HTTP.

---

## 3. Capas en detalle

### 3.1 Domain (`CrudService.Domain`)

**Sin dependencias NuGet externas.**

Contiene las entidades del negocio y los contratos (interfaces) que la infraestructura debe cumplir.

```
CrudService.Domain/
├── Entities/
│   ├── Event.cs             ← Entidad evento
│   ├── Ticket.cs            ← Entidad ticket + enum TicketStatus
│   ├── Payment.cs           ← Entidad pago + enum PaymentStatus
│   └── TicketHistory.cs     ← Historial de cambios de estado
├── Interfaces/
│   ├── IEventRepository.cs
│   ├── ITicketRepository.cs
│   ├── IPaymentRepository.cs
│   └── ITicketHistoryRepository.cs
└── Exceptions/
    ├── EventNotFoundException.cs
    └── TicketNotFoundException.cs
```

**Decisión:** los enums `TicketStatus` y `PaymentStatus` están en Domain sin atributos `[PgName]` de Npgsql. Npgsql 6+ convierte automáticamente los valores en snake_case, que coincide con los tipos PostgreSQL (`available`, `reserved`, `paid`, `released`, `cancelled`).

---

### 3.2 Application (`CrudService.Application`)

**Dependencias:** Domain + `Microsoft.Extensions.Logging.Abstractions`.

Contiene los **casos de uso** del sistema. Cada operación tiene su propio directorio con un Command/Query y su Handler.

```
CrudService.Application/
├── Dtos/
│   ├── EventDtos.cs      ← EventDto, CreateEventRequest, UpdateEventRequest
│   └── TicketDtos.cs     ← TicketDto, CreateTicketRequest, UpdateTicketStatusRequest
├── Exceptions/
│   └── InvalidTicketStatusException.cs
└── UseCases/
    ├── Events/
    │   ├── GetAllEvents/          ← GetAllEventsQuery + Handler
    │   ├── GetEventById/          ← GetEventByIdQuery + Handler
    │   ├── CreateEvent/           ← CreateEventCommand + Handler
    │   ├── UpdateEvent/           ← UpdateEventCommand + Handler
    │   └── DeleteEvent/           ← DeleteEventCommand + Handler
    └── Tickets/
        ├── GetTicketsByEvent/     ← GetTicketsByEventQuery + Handler
        ├── GetTicketById/         ← GetTicketByIdQuery + Handler
        ├── CreateTickets/         ← CreateTicketsCommand + Handler
        ├── UpdateTicketStatus/    ← UpdateTicketStatusCommand + Handler
        ├── ReleaseTicket/         ← ReleaseTicketCommand + Handler
        └── GetExpiredTickets/     ← GetExpiredTicketsQuery + Handler
```

**Patrón Use Case (CQRS-lite sin MediatR):**

```csharp
// 1. El Command/Query es un record inmutable — solo datos de entrada
public record CreateEventCommand(string Name, DateTime StartsAt);

// 2. El Handler contiene la lógica — recibe el Command y retorna el resultado
public class CreateEventCommandHandler
{
    private readonly IEventRepository _eventRepository;  // inyectado

    public async Task<EventDto> HandleAsync(CreateEventCommand command)
    {
        var @event = new Event { Name = command.Name, StartsAt = command.StartsAt };
        var created = await _eventRepository.AddAsync(@event);
        return new EventDto { Id = created.Id, Name = created.Name, ... };
    }
}
```

**Por qué CQRS-lite sin MediatR:** MediatR agrega complejidad (reflexión, pipelines) que no se justifica para este servicio. Los Handlers se inyectan directamente en los controladores, lo que hace el código más explícito y fácil de seguir.

---

### 3.3 Infrastructure (`CrudService.Infrastructure`)

**Dependencias:** Domain + Application + EF Core + Npgsql + RabbitMQ.Client + EFCore.NamingConventions.

Implementa los contratos definidos en Domain e incluye los adaptadores de entrada/salida.

```
CrudService.Infrastructure/
├── Persistence/
│   ├── TicketingDbContext.cs          ← EF Core DbContext
│   └── Repositories/
│       ├── EventRepository.cs         ← implementa IEventRepository
│       ├── TicketRepository.cs        ← implementa ITicketRepository
│       ├── PaymentRepository.cs       ← implementa IPaymentRepository
│       └── TicketHistoryRepository.cs ← implementa ITicketHistoryRepository
├── Messaging/
│   ├── RabbitMQSettings.cs            ← configuración de conexión
│   ├── TicketStatusHub.cs             ← Singleton SSE: correlaciona ticketId ↔ conexiones
│   ├── TicketStatusUpdate.cs          ← record de actualización de estado
│   └── TicketStatusConsumer.cs        ← BackgroundService: RabbitMQ → TicketStatusHub
└── DependencyInjection.cs             ← extensión AddInfrastructureServices()
```

**Por qué Messaging va en Infrastructure:** `TicketStatusConsumer` usa RabbitMQ.Client (detalle de infraestructura). `TicketStatusHub` gestiona conexiones SSE en memoria (estado del servidor, no lógica de negocio). Ambos son adaptadores de entrada.

**Por qué el DbContext va en Infrastructure:** EF Core es una herramienta de infraestructura. La lógica de negocio no debe saber cómo se persisten los datos. Los repositorios encapsulan completamente el acceso a datos.

---

### 3.4 Api (`CrudService.Api`)

**Dependencias:** Application + Infrastructure.

Punto de entrada HTTP. Los controladores reciben peticiones HTTP, crean el Command/Query correspondiente y delegan en el Handler.

```
CrudService.Api/
├── Controllers/
│   ├── EventsController.cs  ← inyecta los 5 Event Handlers
│   └── TicketsController.cs ← inyecta los 6 Ticket Handlers + TicketStatusHub
├── Program.cs               ← composición: AddInfrastructureServices() + Swagger + CORS
└── appsettings.json
```

**Patrón en el controlador:**

```csharp
// Antes (N-Layer): inyectaba IEventService (servicio monolítico)
public EventsController(IEventService eventService)

// Ahora (Hexagonal): inyecta cada Handler explícitamente
public EventsController(
    GetAllEventsQueryHandler getAllEventsHandler,
    CreateEventCommandHandler createEventHandler,
    ...)
```

Cada controlador tiene una dependencia explícita con cada caso de uso que usa. Esto hace el código más legible y cada dependencia tiene un propósito concreto.

---

## 4. Tests

```
tests/CrudService.Application.Tests/
├── Events/
│   ├── GetAllEventsQueryHandlerTests.cs
│   ├── GetEventByIdQueryHandlerTests.cs
│   ├── CreateEventCommandHandlerTests.cs
│   ├── UpdateEventCommandHandlerTests.cs
│   └── DeleteEventCommandHandlerTests.cs
└── Tickets/
    ├── GetTicketsByEventQueryHandlerTests.cs
    ├── GetTicketByIdQueryHandlerTests.cs
    ├── CreateTicketsCommandHandlerTests.cs
    ├── UpdateTicketStatusCommandHandlerTests.cs
    ├── ReleaseTicketCommandHandlerTests.cs
    └── GetExpiredTicketsQueryHandlerTests.cs
```

**19 tests — todos pasando.**

Los tests prueban los Handlers de Application directamente. No necesitan base de datos, ni RabbitMQ, ni HTTP porque la capa Application no depende de ninguno de ellos. Se usan mocks con NSubstitute solo para los repositorios (interfaces de Domain).

**Cómo ejecutar:**
```bash
dotnet test tests/CrudService.Application.Tests/
```

---

## 5. Diagrama de dependencias de proyectos

```
CrudService.Api.csproj
    ↓ referencia
CrudService.Infrastructure.csproj ──→ CrudService.Application.csproj ──→ CrudService.Domain.csproj
                                                                                    ↑
                                   CrudService.Application.Tests.csproj ───────────┘
```

Las flechas indican "depende de". El Domain no tiene flechas de entrada dentro de la solución: nada externo lo contamina.

---

## 6. Cómo agregar un nuevo caso de uso

Ejemplo: agregar `GetTicketsByStatus` (obtener todos los tickets por estado).

**Paso 1 — Application:** crear el directorio y los archivos.
```
UseCases/Tickets/GetTicketsByStatus/
├── GetTicketsByStatusQuery.cs      ← public record GetTicketsByStatusQuery(string Status);
└── GetTicketsByStatusQueryHandler.cs
```

**Paso 2 — Domain (si aplica):** agregar el método al repositorio si no existe.
```csharp
// ITicketRepository.cs
Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus status);
```

**Paso 3 — Infrastructure:** implementar en `TicketRepository.cs`.
```csharp
public async Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus status)
    => await _context.Tickets.Where(t => t.Status == status).ToListAsync();
```

**Paso 4 — Infrastructure/DependencyInjection.cs:** registrar el nuevo Handler.
```csharp
services.AddScoped<GetTicketsByStatusQueryHandler>();
```

**Paso 5 — Api:** inyectar el Handler en el controlador y exponer el endpoint.

**Paso 6 — Tests:** agregar `GetTicketsByStatusQueryHandlerTests.cs`.

---

## 7. Cómo construir y ejecutar

```bash
# Compilar toda la solución
dotnet build CrudService.sln

# Ejecutar tests
dotnet test CrudService.sln

# Ejecutar la API localmente
dotnet run --project src/CrudService.Api

# Docker
docker build -t crud-service .
```

---

## 8. Variables de entorno requeridas

| Variable | Descripción | Ejemplo |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | Cadena de conexión a PostgreSQL | `Host=localhost;Database=ticketing;Username=user;Password=pass` |
| `RabbitMQ__Host` | Host de RabbitMQ | `rabbitmq` |
| `RabbitMQ__Port` | Puerto de RabbitMQ | `5672` |
| `RabbitMQ__Username` | Usuario de RabbitMQ | `guest` |
| `RabbitMQ__Password` | Contraseña de RabbitMQ | `guest` |

---

## 9. Decisiones técnicas clave

| Decisión | Por qué |
|----------|---------|
| CQRS-lite sin MediatR | Suficiente para este servicio. MediatR agrega complejidad innecesaria. |
| `ILogger<T>` en Application | Es una abstracción pura (`Microsoft.Extensions.Logging.Abstractions`). No acopla a una implementación concreta. |
| `TicketStatusHub` como Singleton | Necesita sobrevivir múltiples requests HTTP para correlacionar ticketId con conexiones SSE activas. |
| `TicketStatusConsumer` en Infrastructure | Usa RabbitMQ.Client. Es un adaptador de entrada, no lógica de negocio. |
| Repositories Scoped | Comparten el ciclo de vida del DbContext (por request HTTP). Garantiza consistencia transaccional. |
| Handlers Scoped | Dependen de repositorios. Mismo ciclo de vida que el request. |
