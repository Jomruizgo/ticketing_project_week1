# State — Ciclo de vida de la oportunidad de lista de espera

## Problema que resuelve

La oportunidad tiene cinco estados posibles (`pending`, `active`, `consumed`, `expired`, `failed`) con transiciones restringidas. Sin un modelo de estados explícito, las validaciones de transición se dispersan en condicionales por todo el código. Si el negocio agrega un estado intermedio, hay que buscar y modificar cada `if` que asume las transiciones actuales.

## Diagramas UML

Ver [state.drawio](state.drawio) — contiene el diagrama de estados y el diagrama de clases. Abrir con draw.io o VS Code con extensión Draw.io Integration.

## Código declarativo

### Entidad con validación de transiciones (Domain)

```csharp
// Domain/Entities/WaitlistOpportunity.cs
public class WaitlistOpportunity
{
    public OpportunityStatus Status { get; private set; }

    private static readonly Dictionary<OpportunityStatus, HashSet<OpportunityStatus>> AllowedTransitions = new()
    {
        [OpportunityStatus.Pending] = new() { OpportunityStatus.Active, OpportunityStatus.Failed },
        [OpportunityStatus.Active]  = new() { OpportunityStatus.Consumed, OpportunityStatus.Expired },
    };

    public void TransitionTo(OpportunityStatus newStatus)
    {
        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
            throw new InvalidOpportunityTransitionException(Status, newStatus);

        Status = newStatus;

        if (newStatus == OpportunityStatus.Active)
            ActivatedAt = DateTime.UtcNow;
    }
}
```

> **Nota de implementación (HU3)**: La implementación actual usa `public set` en lugar de `private set` para mantener compatibilidad con los tests existentes de HU2 (`GetWaitlistStatusHandlerTests`), que asignan `Status` directamente al construir datos de prueba. La protección de transiciones se garantiza mediante `TransitionTo` en la lógica de negocio.

## Por qué hace el código más escalable

Sin State, las transiciones se validan con `if (status == "active" && newStatus == "consumed")` dispersos en handlers, consumers y repositorios. Agregar un estado intermedio (por ejemplo, `confirming`) obliga a buscar y modificar cada condicional. Con el modelo de transiciones centralizado en la entidad, agregar un estado es una fila en `AllowedTransitions` — la validación ocurre en un solo lugar y cualquier transición inválida se rechaza con una excepción de dominio explícita.
