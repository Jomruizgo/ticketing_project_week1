# Strategy — Política de priorización en lista de espera

## Problema que resuelve

Hoy la regla de asignación es "orden de llegada". Si el negocio decide cambiar a otro criterio (por ejemplo, compradores frecuentes primero, o compradores que ya pagaron en otro evento), la lógica de selección debe cambiar sin modificar el caso de uso de asignación ni el flujo de expiración/reasignación.

## Diagrama de clases UML

Ver [strategy.drawio](strategy.drawio) — abrir con draw.io o VS Code con extensión Draw.io Integration.

## Código declarativo

### Puerto (Domain)

```csharp
// Domain/Interfaces/IPrioritizationStrategy.cs
public interface IPrioritizationStrategy
{
    WaitlistEntry? SelectNextEligible(IReadOnlyList<WaitlistEntry> activeEntries);
}
```

### Implementación concreta (Infrastructure)

```csharp
// Infrastructure/Strategies/FifoStrategy.cs
public class FifoStrategy : IPrioritizationStrategy
{
    public WaitlistEntry? SelectNextEligible(IReadOnlyList<WaitlistEntry> activeEntries)
    {
        return activeEntries
            .OrderBy(e => e.EnrolledAt)
            .FirstOrDefault();
    }
}
```

### Handler (Application)

```csharp
// Application/UseCases/AssignOpportunity/AssignOpportunityHandler.cs
public class AssignOpportunityHandler
{
    private readonly IPrioritizationStrategy _strategy;

    public async Task HandleAsync(AssignOpportunityCommand command)
    {
        var activeEntries = await _entryRepo.GetActiveEntriesByEventAsync(command.EventId);
        var selected = _strategy.SelectNextEligible(activeEntries);

        if (selected is null)
        {
            // No hay elegibles → ticket vuelve al inventario general
            return;
        }

        // Reservar ticket para el seleccionado...
    }
}
```

## Por qué hace el código más escalable

Sin Strategy, el criterio de selección vive como lógica inline en el handler. Cambiar la política requiere modificar el handler directamente. Con Strategy, el handler no sabe qué criterio se usa — solo invoca `SelectNextEligible`. Cambiar de FIFO a otra política es crear una nueva clase y registrarla en DI.
