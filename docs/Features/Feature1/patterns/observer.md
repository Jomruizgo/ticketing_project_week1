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
// Infrastructure/Messaging/OpportunityActivatedObserver.cs
public class OpportunityActivatedObserver : IOpportunityObserver
{
    public async Task OnOpportunityActivatedAsync(WaitlistOpportunity opportunity)
    {
        // Publica waitlist.opportunity.activated al exchange RabbitMQ
        // Publica delay message a q.waitlist.opportunity.delay
    }

    public async Task OnOpportunityExpiredAsync(WaitlistOpportunity opportunity)
    {
        // Pendiente HU6
    }
}

// Infrastructure/Services/EmailNotificationObserver.cs (HU5 — futuro)
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

> **Nota de implementación (HU4)**: La notificación SSE in-app NO se implementa como un `IOpportunityObserver` adicional. En su lugar, un consumer RabbitMQ dedicado (`SseNotificationConsumer`) escucha las routing keys `waitlist.opportunity.activated` y `waitlist.opportunity.expired` (publicadas por `OpportunityActivatedObserver` y el mecanismo de expiración DLX respectivamente) y despacha al hub SSE in-process. Esta decisión soporta escalamiento horizontal: cualquier instancia del CRUD Service que tenga la conexión SSE del comprador puede emitir la notificación. Ver `specs/004-inapp-notification/research.md` R4.

## Por qué hace el código más escalable

Sin Observer, el handler de asignación tendría llamadas directas a RabbitMQ y correo, y el handler de expiración necesitaría sus propias llamadas. Cada canal nuevo requiere modificar ambos handlers (viola OCP). Con Observer, agregar un canal es registrar un nuevo `IOpportunityObserver` en DI — cero cambios en la lógica de asignación o expiración. La notificación SSE, al consumir el evento publicado por el observer vía RabbitMQ, se beneficia además de escalamiento horizontal sin modificar el handler.
