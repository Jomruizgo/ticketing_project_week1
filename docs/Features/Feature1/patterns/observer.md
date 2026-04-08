# Observer — Notificación multicanal ante cambios en el ciclo de vida de una oportunidad

## Problema que resuelve

Cuando una oportunidad cambia de estado (`active`, `expired`), el sistema debe reaccionar en múltiples canales (in-app vía SSE, correo electrónico) sin que la lógica de asignación ni de expiración conozca ni dependa de esos canales. Si mañana el negocio agrega SMS o push notifications, no se debe modificar ningún caso de uso.

## Diagrama de clases UML

Ver [observer.drawio](observer.drawio) — abrir con draw.io o VS Code con extensión Draw.io Integration.

## Código declarativo

### Puerto (Domain)

```csharp
// Domain/Interfaces/IOpportunityObserver.cs
public interface IOpportunityObserver
{
    Task OnOpportunityActivatedAsync(WaitlistOpportunity opportunity);
    Task OnOpportunityExpiredAsync(WaitlistOpportunity opportunity);
}
```

> **Nota de implementación (HU3)**: La interfaz implementada en HU3 solo declara `OnOpportunityActivatedAsync`. El método `OnOpportunityExpiredAsync` se agregará al implementar HU6 (Expiración de oportunidad), junto con sus adaptadores correspondientes.

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
        // Publica opportunity_activated al hub SSE
    }

    public async Task OnOpportunityExpiredAsync(WaitlistOpportunity opportunity)
    {
        // Publica opportunity_expired al hub SSE
    }
}

// Infrastructure/Services/EmailNotificationObserver.cs
public class EmailNotificationObserver : IOpportunityObserver
{
    public async Task OnOpportunityActivatedAsync(WaitlistOpportunity opportunity)
    {
        // Envía correo + registra intento en NotificationDelivery — fallo aislado
    }

    public async Task OnOpportunityExpiredAsync(WaitlistOpportunity opportunity)
    {
        // No-op: no hay correo de expiración en el alcance de esta épica
    }
}
```

## Por qué hace el código más escalable

Sin Observer, el handler de asignación tendría llamadas directas a SSE y correo, y el handler de expiración necesitaría sus propias llamadas a SSE. Cada canal nuevo requiere modificar ambos handlers (viola OCP). Con Observer, agregar un canal es registrar un nuevo `IOpportunityObserver` en DI — cero cambios en la lógica de asignación o expiración.
