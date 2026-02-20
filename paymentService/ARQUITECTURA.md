# ARQUITECTURA: MsPaymentService

> Documento técnico de referencia para el equipo de desarrollo.
> Describe la arquitectura hexagonal implementada, las decisiones tomadas y cómo extender el servicio.

---

## 1. Contexto: de dónde venimos

### Antes — N-Layer (un solo proyecto)

```
MsPaymentService.Worker.csproj
├── Configurations/     ← configuración de RabbitMQ y pagos
├── Data/               ← DbContext + EntityConfigurations
├── Extensions/         ← registro de DI
├── Handlers/           ← handlers de mensajes RabbitMQ
├── Messaging/          ← consumidor y publisher de RabbitMQ
├── Models/             ← entidades, DTOs y eventos
├── Repositories/       ← interfaces e implementaciones mezcladas
├── Services/           ← lógica de negocio
└── Worker.cs + Program.cs
```

**Problema:** toda la lógica de negocio (`PaymentValidationService`) vivía en el mismo proyecto que EF Core, RabbitMQ y el Worker. Los tests tenían que referenciar el proyecto completo para poder probar una sola regla de negocio.

### Después — Arquitectura Hexagonal (4 proyectos)

```
MsPaymentService.sln
├── src/
│   ├── MsPaymentService.Domain/         ← el corazón: entidades e interfaces
│   ├── MsPaymentService.Application/    ← casos de uso (lógica pura)
│   ├── MsPaymentService.Infrastructure/ ← adaptadores: EF Core, RabbitMQ
│   └── MsPaymentService.Worker/         ← punto de entrada: BackgroundService
└── tests/
    └── MsPaymentService.Application.Tests/ ← tests sin dependencias de infraestructura
```

---

## 2. La regla de dependencias

Las dependencias solo van hacia adentro. Nunca al revés.

```
Worker ──────┐
              ↓
Infrastructure ──→ Application ──→ Domain
```

- **Domain** no depende de nadie. No sabe que existe EF Core, ni RabbitMQ.
- **Application** solo depende de Domain. No sabe que existe PostgreSQL.
- **Infrastructure** implementa los contratos definidos en Domain y Application.
- **Worker** orquesta Infrastructure. Es el punto de entrada del servicio.

---

## 3. Capas en detalle

### 3.1 Domain (`MsPaymentService.Domain`)

**Sin dependencias NuGet externas.**

Contiene las entidades del negocio y los contratos (interfaces) que la infraestructura debe cumplir.

```
MsPaymentService.Domain/
├── Entities/
│   ├── Event.cs
│   ├── Ticket.cs            ← + enum TicketStatus (lowercase: available, reserved, paid…)
│   ├── Payment.cs           ← + enum PaymentStatus (lowercase: pending, approved, failed…)
│   └── TicketHistory.cs
└── Interfaces/
    ├── ITicketRepository.cs
    ├── IPaymentRepository.cs
    └── ITicketHistoryRepository.cs
```

**Decisión:** los enums `TicketStatus` y `PaymentStatus` usan valores en minúsculas (`available`, `reserved`, etc.) para coincidir directamente con los tipos nativos de PostgreSQL, sin necesidad de atributos externos.

---

### 3.2 Application (`MsPaymentService.Application`)

**Dependencias:** Domain + `Microsoft.Extensions.Logging.Abstractions`.

Contiene los **casos de uso** del sistema. La lógica del antiguo `PaymentValidationService` se convierte en dos CommandHandlers independientes.

```
MsPaymentService.Application/
├── Dtos/
│   ├── ValidationResult.cs   ← resultado de procesamiento (Success/Failure/AlreadyProcessed)
│   └── PaymentResponse.cs
├── Events/
│   ├── PaymentApprovedEvent.cs      ← mensaje RabbitMQ: pago aprobado
│   ├── PaymentRejectedEvent.cs      ← mensaje RabbitMQ: pago rechazado
│   ├── PaymentRequestedEvent.cs     ← mensaje RabbitMQ: solicitud de pago
│   ├── TicketStatusChangedEvent.cs  ← mensaje RabbitMQ: estado cambiado
│   └── TicketPaymentEvent.cs
├── Interfaces/
│   └── ITicketStateService.cs       ← puerto de salida para transiciones de estado
└── UseCases/
    ├── ProcessApprovedPayment/   ← ProcessApprovedPaymentCommand + Handler
    └── ProcessRejectedPayment/   ← ProcessRejectedPaymentCommand + Handler
```

**Patrón Use Case (CQRS-lite sin MediatR):**

```csharp
// 1. El Command es un record inmutable — solo datos de entrada
public record ProcessApprovedPaymentCommand(
    long TicketId, long EventId, int AmountCents,
    string Currency, string PaymentBy, string TransactionRef, DateTime ApprovedAt);

// 2. El Handler contiene la lógica — recibe el Command y retorna ValidationResult
public class ProcessApprovedPaymentCommandHandler
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITicketStateService _stateService;

    public async Task<ValidationResult> HandleAsync(ProcessApprovedPaymentCommand command)
    {
        // 1. Verificar idempotencia
        // 2. Validar estado del ticket
        // 3. Validar TTL (5 minutos)
        // 4. Crear/obtener payment
        // 5. Transicionar a Paid
    }
}
```

**ITicketStateService como puerto de Application:** el servicio de transiciones usa `PaymentDbContext` (EF Core) directamente — es detalle de infraestructura. Por eso la interfaz vive en Application (define el contrato) y la implementación en Infrastructure.

---

### 3.3 Infrastructure (`MsPaymentService.Infrastructure`)

**Dependencias:** Domain + Application + EF Core + Npgsql + RabbitMQ.Client.

Implementa los contratos definidos en Domain e Application, y contiene todos los adaptadores de mensajería.

```
MsPaymentService.Infrastructure/
├── Persistence/
│   ├── PaymentDbContext.cs
│   ├── EntityConfigurations/   ← configuración EF Core de cada entidad
│   └── Repositories/
│       ├── TicketRepository.cs         ← implementa ITicketRepository
│       ├── PaymentRepository.cs        ← implementa IPaymentRepository
│       └── TicketHistoryRepository.cs  ← implementa ITicketHistoryRepository
├── Services/
│   └── TicketStateService.cs           ← implementa ITicketStateService (usa DbContext)
├── Messaging/
│   ├── RabbitMQ/
│   │   ├── RabbitMQConnection.cs       ← gestión de conexión/canal
│   │   └── TicketPaymentConsumer.cs    ← consume colas y delega en el dispatcher
│   └── StatusChangedPublisher.cs       ← IStatusChangedPublisher + implementación
├── Handlers/
│   ├── IPaymentEventHandler.cs         ← contrato para handlers de mensajes
│   ├── IPaymentEventDispatcher.cs      ← contrato del dispatcher
│   ├── PaymentEventDispatcherImpl.cs   ← enruta por routing key
│   ├── PaymentApprovedEventHandler.cs  ← JSON → ProcessApprovedPaymentCommand → Handler
│   ├── PaymentRejectedEventHandler.cs  ← JSON → ProcessRejectedPaymentCommand → Handler
│   └── PaymentRequestedEventHandler.cs ← simula gateway de pagos (80% aprobación)
├── Configurations/
│   ├── RabbitMQSettings.cs
│   └── PaymentSettings.cs
└── DependencyInjection.cs              ← extensión AddInfrastructureServices()
```

**Por qué los Handlers van en Infrastructure:** deserializan JSON (detalle técnico), crean Commands para la capa Application, y publican eventos de vuelta a RabbitMQ. Son "adaptadores de entrada" — conocen el protocolo de mensajería pero no la lógica de negocio.

**Por qué TicketStateService va en Infrastructure:** usa `PaymentDbContext` directamente para manejar transacciones (BEGIN/COMMIT/ROLLBACK) y bloqueos pesimistas (SELECT FOR UPDATE). Es un detalle de persistencia, no lógica de negocio.

---

### 3.4 Worker (`MsPaymentService.Worker`)

**Dependencias:** Application + Infrastructure.

Punto de entrada del servicio. Solo contiene el `BackgroundService` y la configuración del host.

```
MsPaymentService.Worker/
├── Worker.cs       ← inicia los consumidores de las 3 colas con retry
├── Program.cs      ← composición: AddInfrastructureServices() + AddHostedService
└── appsettings.json
```

**Worker.cs** solo hace una cosa: intentar conectarse a RabbitMQ y arrancar los consumidores. Si RabbitMQ no está disponible, reintenta hasta 24 veces (2 minutos total).

---

## 4. Flujo de un mensaje

```
RabbitMQ
    ↓
TicketPaymentConsumer (Infrastructure)
    ↓ DispatchAsync(routingKey, json)
IPaymentEventDispatcher → PaymentApprovedEventHandler (Infrastructure)
    ↓ Deserializa JSON → crea ProcessApprovedPaymentCommand
ProcessApprovedPaymentCommandHandler (Application)
    ↓ Valida idempotencia, TTL, estado
    ↓ Llama ITicketStateService.TransitionToPaidAsync
TicketStateService (Infrastructure) — usa PaymentDbContext
    ↓ transacción: UPDATE ticket, UPDATE payment, INSERT history
    ↑ ValidationResult.Success()
PaymentApprovedEventHandler
    ↓ IStatusChangedPublisher.Publish(ticketId, "paid")
RabbitMQ ← ticket.status.changed
```

---

## 5. Tests

```
tests/MsPaymentService.Application.Tests/
├── ProcessApprovedPaymentCommandHandlerTests.cs   ← 10 tests (lógica de pago aprobado + TTL)
├── ProcessRejectedPaymentCommandHandlerTests.cs   ← 4 tests (lógica de pago rechazado)
├── PaymentApprovedEventHandlerTests.cs            ← 3 tests (adaptador Infrastructure)
├── PaymentRejectedEventHandlerTests.cs            ← 2 tests (adaptador Infrastructure)
└── PaymentEventDispatcherImplTests.cs             ← 3 tests (dispatcher por routing key)
```

**25 tests — todos pasando.**

Los Application tests prueban los CommandHandlers directamente, mockeando solo las interfaces de Domain (`ITicketRepository`, `IPaymentRepository`, `ITicketStateService`). Los Infrastructure handler tests instancian los CommandHandlers con mocks de repositorios.

**Cómo ejecutar:**
```bash
dotnet test tests/MsPaymentService.Application.Tests/
```

---

## 6. Diagrama de dependencias de proyectos

```
MsPaymentService.Worker.csproj
    ↓ referencia
MsPaymentService.Infrastructure.csproj ──→ MsPaymentService.Application.csproj ──→ MsPaymentService.Domain.csproj
                                                                                              ↑
                           MsPaymentService.Application.Tests.csproj ──────────────────────────┘
                           (también referencia Infrastructure para tests de handlers)
```

---

## 7. Cómo agregar un nuevo tipo de evento

Ejemplo: agregar `PaymentExpiredEvent` (pago expirado por timeout del sistema).

**Paso 1 — Application:** crear el evento y el command.
```
Events/PaymentExpiredEvent.cs
UseCases/ProcessExpiredPayment/
    ProcessExpiredPaymentCommand.cs
    ProcessExpiredPaymentCommandHandler.cs
```

**Paso 2 — Infrastructure:** crear el handler adaptador.
```csharp
// Handlers/PaymentExpiredEventHandler.cs
public class PaymentExpiredEventHandler : IPaymentEventHandler
{
    public string QueueName => _settings.ExpiredQueueName;
    public async Task<ValidationResult> HandleAsync(string json, ...)
    {
        var evt = JsonSerializer.Deserialize<PaymentExpiredEvent>(json);
        var command = new ProcessExpiredPaymentCommand(evt.TicketId, ...);
        return await _commandHandler.HandleAsync(command);
    }
}
```

**Paso 3 — Infrastructure/DependencyInjection.cs:** registrar el nuevo handler.
```csharp
services.AddScoped<ProcessExpiredPaymentCommandHandler>();
services.AddScoped<IPaymentEventHandler, PaymentExpiredEventHandler>();
```

**Paso 4 — Configurations/RabbitMQSettings.cs:** agregar la nueva cola.

**Paso 5 — Worker.cs:** iniciar el consumo de la nueva cola.

**Paso 6 — Tests:** agregar `ProcessExpiredPaymentCommandHandlerTests.cs`.

---

## 8. Cómo construir y ejecutar

```bash
# Compilar toda la solución
dotnet build MsPaymentService.sln

# Ejecutar tests
dotnet test MsPaymentService.sln

# Docker
docker build -t ms-payment-service .
```

---

## 9. Variables de entorno requeridas

| Variable | Descripción | Ejemplo |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | Cadena de conexión PostgreSQL | `Host=localhost;Database=ticketing;Username=user;Password=pass` |
| `RabbitMQ__HostName` | Host de RabbitMQ | `rabbitmq` |
| `RabbitMQ__Port` | Puerto de RabbitMQ | `5672` |
| `RabbitMQ__UserName` | Usuario de RabbitMQ | `guest` |
| `RabbitMQ__Password` | Contraseña de RabbitMQ | `guest` |
| `RabbitMQ__ApprovedQueueName` | Cola de pagos aprobados | `ticket.payments.approved` |
| `RabbitMQ__RejectedQueueName` | Cola de pagos rechazados | `ticket.payments.rejected` |

---

## 10. Decisiones técnicas clave

| Decisión | Por qué |
|----------|---------|
| CQRS-lite sin MediatR | Suficiente para 2 casos de uso. MediatR agrega complejidad innecesaria. |
| `ITicketStateService` en Application | Es un puerto de salida que la Application necesita. La implementación con transacciones EF Core va en Infrastructure. |
| Handlers en Infrastructure | Deserializan JSON (protocolo RabbitMQ) y publican eventos de vuelta. Son adaptadores, no lógica de negocio. |
| `IStatusChangedPublisher` Singleton | Reutiliza la misma conexión RabbitMQ para publicar eventos de cambio de estado. |
| Repositorios Scoped | Comparten el ciclo de vida del DbContext (por scope de DI). Garantiza consistencia transaccional. |
| CommandHandlers Scoped | Dependen de repositorios Scoped. Mismo ciclo de vida. |
| Enums en minúsculas | Los valores `available`, `reserved`, `paid`, etc. coinciden directamente con los tipos nativos de PostgreSQL. |
