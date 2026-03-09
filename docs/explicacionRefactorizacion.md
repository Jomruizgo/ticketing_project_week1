# Guía de Comprensión: Refactorización a Arquitectura Hexagonal

> **Para quién es este documento:** Para alguien sin experiencia previa en arquitectura de software que necesita entender qué es la Arquitectura Hexagonal, por qué la estamos aplicando en este proyecto, y qué decisión se tomó en cada paso del refactor.

---

## Tabla de Contenido

1. [¿Por qué refactorizamos?](#1-por-qué-refactorizamos)
2. [¿Qué es la Arquitectura Hexagonal?](#2-qué-es-la-arquitectura-hexagonal)
3. [Las 4 capas explicadas desde cero](#3-las-4-capas-explicadas-desde-cero)
4. [La regla de oro de las dependencias](#4-la-regla-de-oro-de-las-dependencias)
5. [Qué era antes (N-Layer)](#5-qué-era-antes-n-layer)
6. [Qué es ahora (Hexagonal)](#6-qué-es-ahora-hexagonal)
7. [El patrón Use Case explicado](#7-el-patrón-use-case-explicado)
8. [Refactor del CRUD Service: decisión por decisión](#8-refactor-del-crud-service-decisión-por-decisión)
9. [Refactor del PaymentService: decisión por decisión](#9-refactor-del-paymentservice-decisión-por-decisión)
10. [Qué NO cambia con el refactor](#10-qué-no-cambia-con-el-refactor)
11. [Beneficios concretos que obtenemos](#11-beneficios-concretos-que-obtenemos)
12. [Glosario de términos nuevos](#12-glosario-de-términos-nuevos)

---

## 1. ¿Por qué refactorizamos?

Antes de hablar de soluciones, entendamos el problema.

### El estado actual de crud_service (N-Layer)

Todo el código vive en un solo proyecto `.csproj`:

```
crud_service/
├── Controllers/      ← Recibe HTTP
├── Services/         ← Lógica de negocio
├── Repositories/     ← Interfaces de BD
├── Data/             ← Implementación de BD
└── Models/           ← Entidades y DTOs
```

**¿Cuál es el problema?** Veamos el `EventService.cs`:

```csharp
public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly ITicketRepository _ticketRepository;
    // ...

    public async Task<EventDto> CreateEventAsync(CreateEventRequest request)
    {
        var @event = new Event { Name = request.Name, StartsAt = request.StartsAt };
        var created = await _eventRepository.AddAsync(@event);
        return MapToDto(created);
    }

    public async Task<EventDto> UpdateEventAsync(long id, UpdateEventRequest request)
    {
        var @event = await _eventRepository.GetByIdAsync(id);
        if (@event == null)
            throw new KeyNotFoundException($"Evento {id} no encontrado");
        // más lógica...
    }

    // 5 métodos más aquí...
    private EventDto MapToDto(Event @event) { ... }
}
```

Este archivo tiene **todas las operaciones de eventos mezcladas**: obtener, crear, actualizar, eliminar, mapear. Si el servicio crece y se agregan 10 operaciones más, este archivo se vuelve gigante e imposible de mantener.

Además, la interfaz `IEventService` está en el **mismo archivo** que la implementación. Esto viola el principio de que una clase debe tener una sola razón para cambiar.

### Los 3 problemas principales

**Problema 1: Todo en un solo lugar**
Un cambio en cómo se guardan los datos (de PostgreSQL a otro motor) implica modificar toda la capa de servicios, porque Services conoce detalles de Data.

**Problema 2: Difícil de testear**
Para probar `CreateEventAsync`, necesitas instanciar el servicio completo con todos sus repositorios. No puedes probar la lógica de negocio de forma aislada.

**Problema 3: Inconsistencia con otros servicios**
`ReservationService` ya tiene arquitectura hexagonal bien implementada. `crud_service` y `paymentService` usan un estilo diferente. Cualquier nuevo desarrollador tiene que entender 3 estructuras distintas.

### La solución: Arquitectura Hexagonal según el estándar Sofka (DA_ADE08)

El documento `DA_ADE08_Anexo Estructura de proyectos .NET` establece que los servicios en Sofka deben seguir la **Estructura Clean Code (DDD)**, que es esencialmente arquitectura hexagonal organizada en proyectos separados.

---

## 2. ¿Qué es la Arquitectura Hexagonal?

### La idea central

Imagina una ciudad medieval con su castillo. El castillo tiene murallas que lo protegen del exterior. Para entrar al castillo, tienes que pasar por la puerta oficial — no puedes entrar por cualquier lado.

En arquitectura hexagonal:
- **El castillo** = El dominio (tus reglas de negocio)
- **Las murallas** = Las interfaces (contratos)
- **La puerta** = Los puertos (puntos de entrada/salida definidos)
- **Los mercaderes** = La infraestructura (base de datos, RabbitMQ, HTTP)

```
                        ┌───────────────────────────────────────┐
                        │           INFRAESTRUCTURA             │
                        │  (lo que cambia: PostgreSQL, RabbitMQ,│
                        │   HTTP, archivos, APIs externas...)    │
                        │                                       │
                        │    ┌───────────────────────────────┐  │
                        │    │         APLICACIÓN            │  │
                        │    │  (orquesta los casos de uso)  │  │
                        │    │                               │  │
                        │    │    ┌───────────────────┐      │  │
                        │    │    │     DOMINIO        │      │  │
                        │    │    │  (el negocio puro) │      │  │
                        │    │    │  Nunca depende de  │      │  │
                        │    │    │  nada exterior     │      │  │
                        │    │    └───────────────────┘      │  │
                        │    └───────────────────────────────┘  │
                        └───────────────────────────────────────┘
```

### ¿Por qué "Hexagonal"?

Su inventor, Alistair Cockburn, la dibujó como un hexágono para mostrar que un sistema puede tener múltiples "puertos" (conexiones al mundo exterior) en cualquier lado, y todos deben respetarse del mismo modo.

```
        HTTP         Tests
          ↓            ↓
    ┌─────────────────────────┐
    │                         │
    │     ┌───────────────┐   │ ← PostgreSQL
    │     │   DOMINIO     │   │
    │     │   (negocio)   │   │ ← RabbitMQ
    │     └───────────────┘   │
    │                         │
    └─────────────────────────┘
          ↑            ↑
       CLI          Email
```

Todos los "enchufes" (HTTP, RabbitMQ, Base de Datos, CLI) son intercambiables. El dominio nunca se entera de cuál enchufe está conectado.

---

## 3. Las 4 capas explicadas desde cero

### Capa 1: Domain (Dominio)

**¿Qué es?** El corazón del sistema. Contiene las reglas de negocio puras, sin tecnología.

**Analogía:** Es el cerebro de un médico. El médico sabe diagnosticar sin importar si usa papel o computadora, si está en Colombia o España. Su conocimiento (negocio) es independiente de las herramientas.

**¿Qué contiene?**
- **Entities**: Las "cosas" del negocio: `Event`, `Ticket`, `Payment`
- **Interfaces**: Los contratos que la Infraestructura debe cumplir: `IEventRepository`, `ITicketRepository`
- **Exceptions**: Errores específicos del negocio: `EventNotFoundException`, `TicketNotAvailableException`

**Regla fundamental:** Esta capa **NO importa nada externo**. Cero referencias a Entity Framework, RabbitMQ, HTTP o cualquier tecnología. Solo C# puro.

```csharp
// ✅ CORRECTO - Solo C# puro, sin tecnología
namespace CrudService.Domain.Entities;

public class Event
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
```

```csharp
// ✅ CORRECTO - La interfaz define el contrato, no la implementación
namespace CrudService.Domain.Interfaces;

public interface IEventRepository
{
    Task<IEnumerable<Event>> GetAllAsync();
    Task<Event?> GetByIdAsync(long id);
    Task<Event> AddAsync(Event @event);
    Task<Event> UpdateAsync(Event @event);
    Task<bool> DeleteAsync(long id);
}
```

---

### Capa 2: Application (Aplicación)

**¿Qué es?** La capa que orquesta los casos de uso. Sabe qué hacer con el dominio para satisfacer una petición del usuario.

**Analogía:** Es el asistente del médico. Sabe que para atender a un paciente debe: 1) verificar si tiene cita, 2) preparar la historia clínica, 3) llevar al paciente a la sala. Coordina el proceso usando las herramientas del médico (dominio), sin ser él mismo el médico.

**¿Qué contiene?**
- **UseCases**: Una carpeta por operación (GetAllEvents, CreateEvent, etc.)
  - `[Operacion]Command.cs` o `[Operacion]Query.cs` → los datos de entrada
  - `[Operacion]CommandHandler.cs` o `[Operacion]QueryHandler.cs` → la lógica
  - `[Operacion]Response.cs` → los datos de salida
- **Dtos**: Objetos para transferir datos entre capas
- **Interfaces**: Contratos que la Infraestructura debe implementar (ej: `ITicketStateService`)
- **Exceptions**: Errores de lógica de aplicación

**Regla fundamental:** Depende del Dominio. **NUNCA** depende de la Infraestructura.

```csharp
// UseCases/Events/CreateEvent/CreateEventCommand.cs
// → Los datos que entran
public record CreateEventCommand(string Name, DateTime StartsAt);

// UseCases/Events/CreateEvent/CreateEventCommandHandler.cs
// → La lógica
public class CreateEventCommandHandler
{
    private readonly IEventRepository _repo;  // ← Usa la interfaz del DOMINIO

    public CreateEventCommandHandler(IEventRepository repo)
    {
        _repo = repo;
    }

    public async Task<CreateEventResponse> HandleAsync(CreateEventCommand command)
    {
        var @event = new Event { Name = command.Name, StartsAt = command.StartsAt };
        var created = await _repo.AddAsync(@event);
        return new CreateEventResponse(created.Id, created.Name, created.StartsAt);
    }
}
```

---

### Capa 3: Infrastructure (Infraestructura)

**¿Qué es?** Todo lo que depende de tecnología específica: bases de datos, mensajería, APIs externas.

**Analogía:** Son las herramientas físicas del consultorio: el computador, el estetoscopio, la impresora. Pueden cambiar (de computador viejo a nuevo) sin que el conocimiento del médico cambie.

**¿Qué contiene?**
- **Persistence**: `DbContext` y repositorios concretos (implementan las interfaces del Dominio)
- **Messaging**: Adaptadores de RabbitMQ, Kafka, etc.
- **Services**: Implementaciones de servicios externos
- **DependencyInjection.cs**: Registro de todas las dependencias

**Regla fundamental:** Implementa las interfaces del Dominio y la Aplicación. Depende de ambos, pero ellos no dependen de ella.

```csharp
// Infrastructure/Persistence/Repositories/EventRepository.cs
// → Implementa la interfaz del DOMINIO usando Entity Framework (tecnología concreta)
public class EventRepository : IEventRepository  // ← IEventRepository viene del DOMINIO
{
    private readonly TicketingDbContext _context;  // ← Entity Framework (tecnología)

    public EventRepository(TicketingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Event>> GetAllAsync()
    {
        return await _context.Events.Include(e => e.Tickets).ToListAsync();
    }
    // ...
}
```

---

### Capa 4: Api / Worker (Presentación)

**¿Qué es?** El punto de entrada al sistema. Para el CRUD Service es la capa HTTP (controllers). Para el PaymentService es el worker que escucha RabbitMQ.

**Analogía:** Es la recepción del consultorio. Recibe a los pacientes (peticiones), los dirige al área correcta, y les entrega el resultado. No diagnostica; solo recibe y entrega.

**¿Qué contiene?**
- **Controllers**: Reciben HTTP, llaman a los handlers de Application, devuelven respuesta
- **Program.cs**: Punto de entrada, configura la aplicación
- **appsettings.json**: Configuración

**Regla fundamental:** Solo llama a la capa Application. No tiene lógica de negocio.

```csharp
// Api/Controllers/EventsController.cs
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly GetAllEventsQueryHandler _getAllHandler;
    private readonly CreateEventCommandHandler _createHandler;
    // ...

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents()
    {
        // El controller solo coordina: llama al handler y devuelve resultado
        var events = await _getAllHandler.HandleAsync(new GetAllEventsQuery());
        return Ok(events);
    }

    [HttpPost]
    public async Task<ActionResult<EventDto>> CreateEvent([FromBody] CreateEventRequest request)
    {
        var command = new CreateEventCommand(request.Name, request.StartsAt);
        var response = await _createHandler.HandleAsync(command);
        return CreatedAtAction(nameof(GetEvent), new { id = response.Id }, response);
    }
}
```

---

## 4. La regla de oro de las dependencias

Esta es **la** regla más importante de la arquitectura hexagonal:

> **Las dependencias siempre apuntan hacia adentro. El exterior conoce al interior, nunca al revés.**

```
Api/Worker
    ↓ depende de
Application
    ↓ depende de
Domain
    ↑ NO depende de nada

Infrastructure
    ↓ depende de
Application (para implementar sus interfaces)
    ↓ depende de
Domain (para implementar sus repositorios)
```

**¿Por qué es importante esta regla?**

Si el Dominio no depende de nada externo, puedes:
- Cambiar de PostgreSQL a MongoDB sin tocar el Dominio ni la Application
- Cambiar de RabbitMQ a Kafka sin tocar el Dominio ni la Application
- Testear el Dominio y la Application sin levantar ninguna base de datos

**Visualización de las dependencias:**

```
┌─────────────────────────────────────────────────────────┐
│  Infrastructure  →  Application  →  Domain              │
│       ↑                  ↑              ↑               │
│  (implementa)       (orquesta)     (define)             │
│                                                         │
│  Api/Worker      →  Application                         │
│       ↑                  ↑                              │
│  (llama)          (usa handlers)                        │
└─────────────────────────────────────────────────────────┘

PROHIBIDO:
Domain  →  Infrastructure   ❌ (el negocio no puede conocer la BD)
Domain  →  Application      ❌ (el negocio no puede conocer los casos de uso)
Application → Infrastructure ❌ (la lógica no puede conocer Entity Framework)
```

---

## 5. Qué era antes (N-Layer)

El `crud_service` original tenía esta estructura (todo en un solo proyecto):

```
crud_service/  (UN SOLO PROYECTO)
├── Controllers/           ← HTTP
│   ├── EventsController.cs
│   └── TicketsController.cs
├── Services/              ← Lógica de negocio
│   ├── EventService.cs    ← Contiene IEventService + EventService juntos
│   └── TicketService.cs   ← Contiene ITicketService + TicketService juntos
├── Repositories/          ← Interfaces de datos
│   └── IRepositories.cs   ← TODAS las interfaces en UN archivo
├── Data/                  ← Implementación de datos
│   ├── TicketingDbContext.cs
│   └── RepositoriesImplementation.cs  ← TODOS los repositorios en UN archivo
├── Models/
│   ├── Entities/          ← Event, Ticket, Payment, TicketHistory
│   └── DTOs/              ← EventDto, TicketDto, etc.
└── Extensions/
    └── ServiceExtensions.cs
```

**Problemas de esta estructura:**

1. **`IRepositories.cs` tiene 4 interfaces en un archivo.** Si cambias `ITicketRepository`, tocas el mismo archivo que `IEventRepository`. Conflictos en git, confusión.

2. **`RepositoriesImplementation.cs` tiene 4 clases en un archivo.** Más de 300 líneas en un solo archivo.

3. **`EventService.cs` tiene la interfaz y la implementación juntas.** Si alguien quiere solo la interfaz, importa todo el archivo con la implementación.

4. **La lógica de "liberar ticket" está en el Controller AND en el Service.** Responsabilidad duplicada.

5. **No hay separación clara de capas.** Todo está en el mismo proyecto, lo que significa que un `Controller` podría técnicamente llamar directamente a `Data/` ignorando el `Service`.

---

## 6. Qué es ahora (Hexagonal)

Después del refactor, cada servicio tiene **4 proyectos separados**:

```
crud_service/
├── CrudService.sln                    ← Agrupa los 4 proyectos
├── src/
│   ├── CrudService.Domain/            ← PROYECTO 1: Solo negocio puro
│   │   ├── Entities/
│   │   │   ├── Event.cs
│   │   │   ├── Ticket.cs
│   │   │   ├── Payment.cs
│   │   │   └── TicketHistory.cs
│   │   ├── Interfaces/
│   │   │   ├── IEventRepository.cs    ← 1 interfaz = 1 archivo
│   │   │   ├── ITicketRepository.cs
│   │   │   ├── IPaymentRepository.cs
│   │   │   └── ITicketHistoryRepository.cs
│   │   └── Exceptions/
│   │       ├── EventNotFoundException.cs
│   │       └── TicketNotFoundException.cs
│   │
│   ├── CrudService.Application/       ← PROYECTO 2: Casos de uso
│   │   ├── UseCases/
│   │   │   ├── Events/
│   │   │   │   ├── GetAllEvents/
│   │   │   │   │   ├── GetAllEventsQuery.cs
│   │   │   │   │   ├── GetAllEventsQueryHandler.cs
│   │   │   │   │   └── GetAllEventsResponse.cs
│   │   │   │   ├── CreateEvent/
│   │   │   │   │   ├── CreateEventCommand.cs
│   │   │   │   │   ├── CreateEventCommandHandler.cs
│   │   │   │   │   └── CreateEventResponse.cs
│   │   │   │   └── ... (5 casos de uso más)
│   │   │   └── Tickets/
│   │   │       └── ... (6 casos de uso)
│   │   └── Dtos/
│   │       ├── EventDto.cs
│   │       └── TicketDto.cs
│   │
│   ├── CrudService.Infrastructure/    ← PROYECTO 3: Tecnología concreta
│   │   ├── Persistence/
│   │   │   ├── TicketingDbContext.cs
│   │   │   └── Repositories/
│   │   │       ├── EventRepository.cs     ← Implementa IEventRepository
│   │   │       ├── TicketRepository.cs    ← Implementa ITicketRepository
│   │   │       ├── PaymentRepository.cs
│   │   │       └── TicketHistoryRepository.cs
│   │   └── DependencyInjection.cs     ← Conecta interfaces con implementaciones
│   │
│   └── CrudService.Api/               ← PROYECTO 4: Punto de entrada HTTP
│       ├── Controllers/
│       │   ├── EventsController.cs    ← Llama a handlers de Application
│       │   └── TicketsController.cs
│       └── Program.cs
│
└── tests/
    ├── CrudService.Domain.Tests/
    └── CrudService.Application.Tests/ ← Tests de lógica de negocio
```

**¿Por qué proyectos separados y no solo carpetas?**

En .NET, proyectos separados (`.csproj`) permiten **controlar las dependencias a nivel de compilador**. Si `CrudService.Domain.csproj` no tiene referencia a Entity Framework, es **físicamente imposible** que alguien en el Dominio use Entity Framework. El compilador lo rechaza.

Con solo carpetas, todo está en el mismo proyecto y cualquier clase puede llamar a cualquier otra. No hay protección.

---

## 7. El patrón Use Case explicado

Este es el concepto más nuevo del refactor. Antes teníamos servicios grandes con múltiples métodos. Ahora cada operación tiene su propia clase.

### ¿Por qué?

Piensa en una navaja suiza vs un set de cuchillos de cocina. La navaja suiza tiene muchas funciones en un solo objeto (el Service). El set de cuchillos tiene una herramienta específica para cada tarea (los Use Cases). El chef profesional usa el set de cuchillos porque:
- Cada herramienta está optimizada para su tarea
- Puedes cambiar un cuchillo sin afectar los demás
- Es más fácil entender qué hace cada uno

### Estructura de un Use Case

Todo Use Case tiene exactamente 3 archivos:

**1. Command o Query (los datos de entrada)**
```csharp
// Command = modifica datos (Create, Update, Delete)
// Query = solo lee datos (Get)

// CreateEventCommand.cs
public record CreateEventCommand(string Name, DateTime StartsAt);

// GetAllEventsQuery.cs
public record GetAllEventsQuery();  // Sin parámetros porque trae todo

// GetEventByIdQuery.cs
public record GetEventByIdQuery(long Id);
```

> **¿Qué es un `record`?** Es una clase especial de C# que es inmutable (no se puede modificar después de creada) y tiene comparación por valor. Perfecta para objetos de datos que solo se crean y se pasan.

**2. Handler (la lógica)**
```csharp
// CreateEventCommandHandler.cs
public class CreateEventCommandHandler
{
    private readonly IEventRepository _repository;

    // El Handler recibe sus dependencias por el constructor (Dependency Injection)
    public CreateEventCommandHandler(IEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateEventResponse> HandleAsync(CreateEventCommand command)
    {
        // Aquí va TODA la lógica de negocio de esta operación específica
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("El nombre es requerido");

        var @event = new Event
        {
            Name = command.Name,
            StartsAt = command.StartsAt
        };

        var created = await _repository.AddAsync(@event);

        return new CreateEventResponse(created.Id, created.Name, created.StartsAt);
    }
}
```

**3. Response (los datos de salida)**
```csharp
// CreateEventResponse.cs
public record CreateEventResponse(long Id, string Name, DateTime StartsAt);
```

### ¿Cómo lo llama el Controller?

```csharp
[HttpPost]
public async Task<ActionResult> CreateEvent([FromBody] CreateEventRequest request)
{
    // 1. Construye el Command con los datos del request HTTP
    var command = new CreateEventCommand(request.Name, request.StartsAt);

    // 2. Lo pasa al Handler
    var response = await _createEventHandler.HandleAsync(command);

    // 3. Devuelve la respuesta HTTP
    return CreatedAtAction(nameof(GetEvent), new { id = response.Id }, response);
}
```

El Controller no tiene lógica de negocio. Solo convierte HTTP → Command → Handler → Response → HTTP.

### La diferencia vs el Service anterior

```
ANTES (Service):                    DESPUÉS (Use Cases):
────────────────                    ────────────────────
EventService.cs                     GetAllEventsQueryHandler.cs
  GetAllEventsAsync()               GetEventByIdQueryHandler.cs
  GetEventByIdAsync()               CreateEventCommandHandler.cs
  CreateEventAsync()                UpdateEventCommandHandler.cs
  UpdateEventAsync()                DeleteEventCommandHandler.cs
  DeleteEventAsync()
  MapToDto()
```

Cada Handler tiene una sola responsabilidad. Si el comportamiento de "crear evento" cambia, solo tocas `CreateEventCommandHandler.cs`. El resto no se ve afectado.

---

## 8. Refactor del CRUD Service: decisión por decisión

### Decisión 1: Las entidades van al Domain

**Antes:** `crud_service/Models/Entities/Event.cs`
**Después:** `CrudService.Domain/Entities/Event.cs`

**¿Por qué?** Las entidades son objetos de negocio. Un `Event` existe en el mundo del negocio (un concierto real) antes de que exista en ninguna base de datos. Por eso pertenecen al Dominio, que es independiente de cualquier tecnología.

### Decisión 2: Las interfaces de repositorios van al Domain

**Antes:** `crud_service/Repositories/IRepositories.cs` (todas juntas)
**Después:**
- `CrudService.Domain/Interfaces/IEventRepository.cs`
- `CrudService.Domain/Interfaces/ITicketRepository.cs`
- `CrudService.Domain/Interfaces/IPaymentRepository.cs`
- `CrudService.Domain/Interfaces/ITicketHistoryRepository.cs`

**¿Por qué al Domain y no a Application?** Porque el Dominio define cómo quiere acceder a sus entidades. Dice "yo necesito poder buscar eventos por ID" sin saber si eso se hace con PostgreSQL, MongoDB, o un archivo de texto. La interfaz es el contrato del Dominio.

**¿Por qué archivos separados?** El PDF dice "1 interfaz = 1 archivo". Esto sigue el **Interface Segregation Principle**: si cambias `ITicketRepository`, no deberías tocar el archivo de `IEventRepository`. Con archivos separados, git te muestra exactamente qué cambió.

### Decisión 3: Los servicios se convierten en Use Cases en Application

**Antes:** `crud_service/Services/EventService.cs` con 5 métodos
**Después:** 5 carpetas en `CrudService.Application/UseCases/Events/`:
- `GetAllEvents/` (Query)
- `GetEventById/` (Query)
- `CreateEvent/` (Command)
- `UpdateEvent/` (Command)
- `DeleteEvent/` (Command)

**¿Por qué Application y no Domain?** Los casos de uso coordinan el dominio pero no son el dominio en sí. "Crear un evento" implica validar datos, llamar al repositorio, y devolver una respuesta. Esa coordinación pertenece a la capa de Aplicación.

**¿Por qué eliminar `IEventService`?** La interfaz del servicio era un intermediario que no aportaba valor real. El Controller llamaba a `IEventService`, que llamaba a `IEventRepository`. Con Use Cases, el Controller llama directamente a los handlers, que son más específicos y testables. Menos capas de abstracción innecesarias.

### Decisión 4: Los DTOs van a Application

**Antes:** `crud_service/Models/DTOs/EventDtos.cs`
**Después:** `CrudService.Application/Dtos/EventDto.cs`

**¿Por qué Application?** Los DTOs son objetos de transferencia entre la Application y el exterior (el Controller). No son entidades de negocio (eso es el Dominio). Son la forma en que la Application comunica resultados hacia afuera.

### Decisión 5: El DbContext y repositorios van a Infrastructure

**Antes:**
- `crud_service/Data/TicketingDbContext.cs`
- `crud_service/Data/RepositoriesImplementation.cs`

**Después:**
- `CrudService.Infrastructure/Persistence/TicketingDbContext.cs`
- `CrudService.Infrastructure/Persistence/Repositories/EventRepository.cs`
- `CrudService.Infrastructure/Persistence/Repositories/TicketRepository.cs`
- (un archivo por repositorio)

**¿Por qué Infrastructure?** Entity Framework es tecnología concreta. PostgreSQL es tecnología concreta. Si mañana cambiamos de PostgreSQL a Cassandra, solo cambia `CrudService.Infrastructure`. El Dominio y la Application no se enteran.

**¿Por qué un archivo por repositorio?** Por las mismas razones que las interfaces: claridad, una sola razón para cambiar, facilidad para navegar el código.

### Decisión 6: Los Controllers se quedan en Api pero cambian cómo llaman

**Antes:** `EventsController` llama a `IEventService`
**Después:** `EventsController` llama a `GetAllEventsQueryHandler`, `CreateEventCommandHandler`, etc.

**¿Por qué?** Los controllers son la "puerta de entrada" HTTP. Pertenecen a la capa de presentación (`CrudService.Api`). Lo que cambia es que en lugar de llamar a un servicio monolítico, llaman directamente a los handlers de los Use Cases.

### Decisión 7: ServiceExtensions.cs se convierte en DependencyInjection.cs en Infrastructure

**Antes:** `crud_service/Extensions/ServiceExtensions.cs`
**Después:** `CrudService.Infrastructure/DependencyInjection.cs`

**¿Por qué Infrastructure?** El registro de dependencias conecta las interfaces con sus implementaciones. Eso es conocimiento de infraestructura: "cuando alguien pida un `IEventRepository`, dale un `EventRepository` con Entity Framework". Esa conexión vive en Infrastructure.

---

## 9. Refactor del PaymentService: decisión por decisión

### Decisión 1: Las entidades van al Domain

**Antes:** `paymentService/Models/Entities/`
**Después:** `MsPaymentService.Domain/Entities/`

Mismo razonamiento que el CRUD Service.

### Decisión 2: Las interfaces de repositorios van al Domain

**Antes:** `paymentService/Repositories/I*.cs`
**Después:** `MsPaymentService.Domain/Interfaces/I*.cs`

Mismo razonamiento.

### Decisión 3: `PaymentValidationService` desaparece — su lógica pasa a Use Cases

Esta es la decisión más importante y diferente del PaymentService.

**Antes:** `Services/PaymentValidationService.cs` contenía toda la lógica de validación en dos métodos gigantes.

**Después:** Esa lógica se divide en dos Use Cases:
- `Application/UseCases/ProcessApprovedPayment/ProcessApprovedPaymentCommandHandler.cs`
- `Application/UseCases/ProcessRejectedPayment/ProcessRejectedPaymentCommandHandler.cs`

**¿Por qué eliminar `IPaymentValidationService`?** Porque era un servicio con demasiadas responsabilidades. Con Use Cases, cada operación tiene su propia clase con su propia lógica. La separación es más clara.

### Decisión 4: `TicketStateService` va a Infrastructure

**Antes:** `Services/TicketStateService.cs`
**Después:** `Infrastructure/Services/TicketStateService.cs`

**¿Por qué Infrastructure?** `TicketStateService` usa directamente el `PaymentDbContext` para manejar transacciones de base de datos. Eso es tecnología concreta (Entity Framework + PostgreSQL). Pertenece a Infrastructure.

La interfaz `ITicketStateService` va a Application (porque la Application necesita saber que existe ese servicio), pero la implementación concreta va a Infrastructure.

### Decisión 5: Los Handlers de RabbitMQ van a Infrastructure

**Antes:** `Handlers/PaymentApprovedEventHandler.cs` y `PaymentRejectedEventHandler.cs` mezclados con lógica de negocio.

**Después:** `Infrastructure/Handlers/PaymentApprovedEventHandler.cs`

**¿Por qué Infrastructure?** Los handlers de RabbitMQ son **adaptadores de entrada**: reciben un mensaje de RabbitMQ (tecnología externa), lo deserializan, y llaman al Use Case correspondiente. Son la "puerta de entrada" desde RabbitMQ, igual que los Controllers son la "puerta de entrada" desde HTTP.

```
                INFRAESTRUCTURA             APLICACIÓN
                ───────────────             ──────────
RabbitMQ msg → PaymentApprovedHandler  →  ProcessApprovedPaymentCommandHandler
                (Infrastructure)           (Application)
                (deserializa JSON)         (lógica de negocio)
                (adaptador de entrada)     (caso de uso puro)
```

### Decisión 6: Los Events de dominio van a Application

**Antes:** `Models/Events/PaymentApprovedEvent.cs`
**Después:** `Application/Events/PaymentApprovedEvent.cs`

**¿Por qué Application?** Los eventos (`PaymentApprovedEvent`, `PaymentRejectedEvent`) son los contratos de mensajería que la Application espera recibir. No son entidades de dominio (no son "Ticket" o "Payment"), sino mensajes que entran al sistema. Viven en Application porque los Use Cases necesitan conocerlos para procesarlos.

---

## 10. Qué NO cambia con el refactor

Es importante entender que este refactor es **solo organizacional**. No cambia:

| Qué | Sigue igual |
|-----|-------------|
| **Comportamiento** | Los endpoints responden exactamente igual |
| **Base de datos** | Mismas tablas, mismas queries |
| **RabbitMQ** | Mismas colas, mismo exchange, mismos routing keys |
| **Docker** | Mismos contenedores, mismo compose.yml |
| **APIs externas** | Mismos endpoints, mismos contratos |
| **Tests** | Los mismos casos de prueba, solo en nueva ubicación |

Lo que SÍ cambia es cómo está **organizado el código**, haciendo que sea más fácil de entender, mantener y extender.

---

## 11. Beneficios concretos que obtenemos

### Beneficio 1: Testabilidad real

**Antes:** Para probar `EventService.CreateEventAsync()`, necesitabas instanciar Entity Framework, conectarte a PostgreSQL, o crear mocks complejos.

**Después:** Para probar `CreateEventCommandHandler.HandleAsync()`, solo necesitas un mock de `IEventRepository`. La lógica de negocio se puede probar completamente sin base de datos.

```csharp
// Test DESPUÉS del refactor
[Fact]
public async Task HandleAsync_ValidCommand_CreatesEvent()
{
    // Arrange
    var mockRepo = Substitute.For<IEventRepository>();  // Mock simple
    var handler = new CreateEventCommandHandler(mockRepo);
    var command = new CreateEventCommand("Concierto", DateTime.UtcNow.AddDays(30));

    mockRepo.AddAsync(Arg.Any<Event>())
            .Returns(new Event { Id = 1, Name = "Concierto" });

    // Act
    var result = await handler.HandleAsync(command);

    // Assert
    Assert.Equal(1, result.Id);
    Assert.Equal("Concierto", result.Name);
}
```

### Beneficio 2: Consistencia entre servicios

Después del refactor, los 3 servicios (CRUD, Payment, Reservation) tienen la misma estructura. Un nuevo desarrollador que entienda uno, entiende todos.

### Beneficio 3: Cambios localizados

Si mañana cambian los requerimientos de "crear evento" (por ejemplo, agregar validación de fecha mínima), solo tocas `CreateEventCommandHandler.cs`. No hay que buscar en un servicio monolítico de 200 líneas dónde está esa lógica.

### Beneficio 4: Protección por compilación

Como son proyectos separados, el compilador te impide violar las reglas:
- No puedes usar Entity Framework en el Dominio (no tiene la referencia)
- No puedes llamar al DbContext desde Application (no tiene la referencia)

### Beneficio 5: Cumplimiento del estándar Sofka (DA_ADE08)

El documento oficial de Sofka establece esta estructura para proyectos .NET. Seguirla garantiza que el proyecto sea reconocible por cualquier desarrollador de Sofka.

---

## 12. Glosario de términos nuevos

| Término | Definición simple |
|---------|------------------|
| **Arquitectura Hexagonal** | Organización del código en capas concéntricas donde el negocio está en el centro y no depende del exterior |
| **Domain** | Capa del centro: contiene entidades, interfaces y excepciones de negocio puro |
| **Application** | Capa intermedia: contiene los casos de uso (qué puede hacer el sistema) |
| **Infrastructure** | Capa exterior: contiene la tecnología concreta (BD, mensajería, APIs externas) |
| **Use Case** | Una operación específica del sistema (Crear Evento, Reservar Ticket) representada como una clase |
| **Command** | Objeto con los datos de entrada de una operación que modifica estado |
| **Query** | Objeto con los datos de entrada de una operación que solo consulta |
| **Handler** | La clase que procesa un Command o Query y contiene la lógica de negocio |
| **Response** | Objeto con los datos de salida de un Use Case |
| **Port** | Interfaz que define cómo se comunica el interior con el exterior |
| **Adapter** | Implementación concreta de un Port (ej: `EventRepository` implementa `IEventRepository`) |
| **DependencyInjection.cs** | Archivo que conecta interfaces (contratos) con sus implementaciones concretas |
| **.sln (Solution)** | Archivo de Visual Studio que agrupa múltiples proyectos .csproj |
| **.csproj** | Archivo de proyecto .NET que define dependencias y configuración de compilación |
| **record** | Tipo especial de clase en C# que es inmutable y se compara por valor |
| **Separation of Concerns** | Principio de que cada parte del sistema debe tener una sola responsabilidad |
| **Interface Segregation** | Principio de que las interfaces deben ser pequeñas y específicas |
| **Single Responsibility** | Principio de que cada clase tiene solo una razón para cambiar |
| **Dependency Inversion** | Principio de que el código de alto nivel no debe depender del de bajo nivel; ambos deben depender de abstracciones |
| **CQRS** | Command Query Responsibility Segregation - separar operaciones de escritura (Commands) de lectura (Queries) |
| **Mock** | Objeto falso que simula el comportamiento de una dependencia real, usado en tests |

---

*Documento generado como apoyo al aprendizaje del refactor arquitectónico del proyecto Ticketing.*
*Fecha: 19/02/2026*
