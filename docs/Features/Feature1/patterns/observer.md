# Observer — Notificación multicanal al activarse una oportunidad

## Problema que resuelve

Cuando una oportunidad pasa a `active`, el sistema debe reaccionar en múltiples canales (in-app vía SSE, correo electrónico) sin que la lógica de asignación conozca ni dependa de esos canales. Si mañana el negocio agrega SMS o push notifications, no se debe modificar el caso de uso de asignación.

## Diagrama de clases UML

Ver [observer.drawio](observer.drawio) — abrir con draw.io o VS Code con extensión Draw.io Integration.

## Código declarativo

### Puerto (Domain)

```csharp
// Domain/Interfaces/IOpportunityObserver.cs
public interface IOpportunityObserver
{
    Task OnOpportunityActivatedAsync(WaitlistOpportunity opportunity);
}
```

### Handler (Application)

```csharp
// Application/UseCases/AssignOpportunity/AssignOpportunityHandler.cs
public class AssignOpportunityHandler
{
    private readonly IEnumerable<IOpportunityObserver> _observers;
    // ... otros puertos

    public async Task HandleAsync(AssignOpportunityCommand command)
    {
        // ... lógica de asignación (buscar elegible, reservar ticket, crear oportunidad)

        foreach (var observer in _observers)
            await observer.OnOpportunityActivatedAsync(opportunity);
    }
}
```

### Adaptadores (Infrastructure)

```csharp
// Infrastructure/Services/SseNotificationObserver.cs
public class SseNotificationObserver : IOpportunityObserver
{
    public async Task OnOpportunityActivatedAsync(WaitlistOpportunity opportunity)
    {
        // Publica al hub SSE — solo eso
    }
}

// Infrastructure/Services/EmailNotificationObserver.cs
public class EmailNotificationObserver : IOpportunityObserver
{
    public async Task OnOpportunityActivatedAsync(WaitlistOpportunity opportunity)
    {
        // Envía correo + registra intento en NotificationDelivery — fallo aislado
    }
}
```

## Por qué hace el código más escalable

Sin Observer, el handler de asignación tendría llamadas directas a SSE y correo. Cada canal nuevo requiere modificar ese handler (viola OCP). Con Observer, agregar un canal es registrar un nuevo `IOpportunityObserver` en DI — cero cambios en la lógica de asignación.
