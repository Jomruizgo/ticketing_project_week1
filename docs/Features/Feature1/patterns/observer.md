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
}
```

> **Estado actual (HU3)**: La interfaz recibe la entidad `WaitlistOpportunity` directamente. `OnOpportunityActivatedAsync` es el único método. El handler inyecta un **solo** `IOpportunityObserver` (no `IEnumerable`), y el DI registra `OpportunityActivatedObserver` como única implementación.
>
> **Refactor planificado (HU5)**: Se refactorizará la interfaz para recibir un record tipado `OpportunityActivatedEvent` (a definir en Domain) que incluya `EventName` resuelto upstream, siguiendo Tell Don't Ask. Se cambiará el handler a `IEnumerable<IOpportunityObserver>` para soportar múltiples observers (RabbitMQ + Email). Se agregará `OnOpportunityExpiredAsync` al implementar HU6.

### Handler (Application)

```csharp
// Application/UseCases/AssignOpportunity/AssignOpportunityHandler.cs
public class AssignOpportunityHandler
{
    private readonly IOpportunityObserver _observer;
    // ... otros puertos

    public async Task HandleAsync(AssignOpportunityCommand command)
    {
        // ... lógica de asignación (buscar elegible, reservar ticket, crear oportunidad)

        await _observer.OnOpportunityActivatedAsync(opportunity);
    }
}
```

> **Estado actual**: El handler inyecta un solo `IOpportunityObserver`. El refactor a `IEnumerable<IOpportunityObserver>` se realizará en HU5 cuando se agregue `EmailNotificationObserver` como segundo observer.

### Adaptadores (Infrastructure)

```csharp
// Infrastructure/Messaging/OpportunityActivatedObserver.cs
public class OpportunityActivatedObserver : IOpportunityObserver
{
    public async Task OnOpportunityActivatedAsync(WaitlistOpportunity opportunity)
    {
        // Construye OpportunityActivatedEvent (class en Infrastructure/Messaging/)
        // Serializa y publica a exchange RabbitMQ "tickets"
        // routing key: waitlist.opportunity.activated
        // Publica delay message a q.waitlist.opportunity.delay
    }
}

// Infrastructure/Services/EmailNotificationObserver.cs (HU5 — futuro)
public class EmailNotificationObserver : IOpportunityObserver
{
    public async Task OnOpportunityActivatedAsync(WaitlistOpportunity opportunity)
    {
        // Crea registro pending en notification_deliveries
        // Envía correo vía IEmailSender
        // Actualiza registro a sent/failed según resultado
        // Fallo aislado: no propaga excepciones
    }
}
```

> **Nota de implementación (HU4)**: La notificación SSE in-app NO se implementa como un `IOpportunityObserver` adicional. En su lugar, un consumer RabbitMQ dedicado (`SseNotificationConsumer`) escucha las routing keys `waitlist.opportunity.activated` y `waitlist.opportunity.expired` (publicadas por `OpportunityActivatedObserver` y el mecanismo de expiración DLX respectivamente) y despacha al hub SSE in-process. Esta decisión soporta escalamiento horizontal: cualquier instancia del CRUD Service que tenga la conexión SSE del comprador puede emitir la notificación. Ver `specs/004-inapp-notification/research.md` R4.

## Por qué hace el código más escalable

Sin Observer, el handler de asignación tendría llamadas directas a RabbitMQ y correo, y el handler de expiración necesitaría sus propias llamadas. Cada canal nuevo requiere modificar ambos handlers (viola OCP). Con Observer, agregar un canal es registrar un nuevo `IOpportunityObserver` en DI — cero cambios en la lógica de asignación o expiración. La notificación SSE, al consumir el evento publicado por el observer vía RabbitMQ, se beneficia además de escalamiento horizontal sin modificar el handler.

El uso de un record tipado (`OpportunityActivatedEvent`) como payload de notificación en vez de la entidad de dominio (`WaitlistOpportunity`) sigue el principio Tell Don’t Ask y el patrón Observer canónico (GoF): el payload contiene toda la información que los observers necesitan para actuar, evitando queries redundantes y desacoplando la interfaz de notificación del modelo interno.
