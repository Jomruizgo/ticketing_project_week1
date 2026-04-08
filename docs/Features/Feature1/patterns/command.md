# Command — Casos de uso como objetos formales

## Problema que resuelve

Las acciones de negocio de la lista de espera (inscribir, asignar oportunidad, expirar oportunidad, notificar por correo) necesitan ser trazables, auditables y testeables de forma aislada. Si la lógica vive directamente en controllers o consumers, no se puede probar sin levantar infraestructura HTTP o RabbitMQ, y la trazabilidad depende de logs genéricos en lugar de objetos con identidad propia.

## Diagrama de clases UML

Ver [command.drawio](command.drawio) — abrir con draw.io o VS Code con extensión Draw.io Integration.

## Código declarativo

### Command y Response (Application)

```csharp
// Application/UseCases/EnrollInWaitlist/EnrollInWaitlistCommand.cs
public record EnrollInWaitlistCommand(long EventId, string BuyerEmail);
```

### Puerto de entrada (Application)

```csharp
// Application/Interfaces/IEnrollInWaitlistUseCase.cs
public interface IEnrollInWaitlistUseCase
{
    Task<WaitlistEntryDto> HandleAsync(EnrollInWaitlistCommand command);
}
```

### Handler (Application)

```csharp
// Application/UseCases/EnrollInWaitlist/EnrollInWaitlistHandler.cs
public class EnrollInWaitlistHandler : IEnrollInWaitlistUseCase
{
    private readonly IWaitlistEntryRepository _entryRepo;
    private readonly IEventRepository _eventRepo;

    public async Task<WaitlistEntryDto> HandleAsync(EnrollInWaitlistCommand command)
    {
        // Validar que el evento existe y la lista sigue vigente
        // Validar unicidad de inscripción activa
        // Crear inscripción
        // Retornar DTO
    }
}
```

### Controller (Api — adaptador de entrada)

```csharp
// Api/Controllers/WaitlistController.cs
[ApiController]
[Route("api/waitlist")]
public class WaitlistController : ControllerBase
{
    private readonly IEnrollInWaitlistUseCase _enrollUseCase;

    [HttpPost("entries")]
    public async Task<IActionResult> PostEntry([FromBody] EnrollRequest request)
    {
        var command = new EnrollInWaitlistCommand(request.EventId, request.BuyerEmail);
        var result = await _enrollUseCase.HandleAsync(command);
        return CreatedAtAction(/* ... */, result);
    }
}
```

## Por qué hace el código más escalable

Sin Command, la lógica de inscripción vive dentro del controller. Testear esa lógica requiere levantar un contexto HTTP. Si un consumer de RabbitMQ necesita reutilizar la misma lógica, hay que duplicarla o extraerla a posteriori. Con Command, el handler es un objeto independiente con su propio input y output. Cualquier adaptador de entrada (controller HTTP, consumer RabbitMQ, test unitario) puede invocarlo a través del puerto de entrada sin saber nada del otro.

## Casos de uso de esta épica

| Caso de uso | Command | Handler | Puerto de entrada |
|---|---|---|---|
| Inscripción | `EnrollInWaitlistCommand` | `EnrollInWaitlistHandler` | `IEnrollInWaitlistUseCase` |
| Consulta de estado | `GetWaitlistStatusQuery` | `GetWaitlistStatusHandler` | `IGetWaitlistStatusUseCase` |
| Asignación de oportunidad | `AssignOpportunityCommand` | `AssignOpportunityHandler` | `IAssignOpportunityUseCase` |
| Consumir oportunidad | `ClaimOpportunityCommand` | `ClaimOpportunityHandler` | `IClaimOpportunityUseCase` |
| Expiración de oportunidad | `ExpireOpportunityCommand` | `ExpireOpportunityHandler` | `IExpireOpportunityUseCase` |
