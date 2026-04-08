# Research: Waitlist Opportunity Status & Claim UI

**Feature**: 008-waitlist-opportunity-ui
**Date**: 2026-04-08

## Unknowns Resolved

### R1: ¿El dominio WaitlistOpportunity ya soporta la transición Active→Consumed?

- **Decision**: Sí, ya está implementado.
- **Rationale**: `WaitlistOpportunity.TransitionTo()` define `AllowedTransitions[Active] = { Consumed, Expired }`. No se requiere cambio en Domain.
- **Alternatives considered**: Agregar método `Claim()` dedicado — rechazado porque `TransitionTo(Consumed)` ya encapsula la validación de transiciones del patrón State.

### R2: ¿Cómo obtener el buyerEmail para validar el claim?

- **Decision**: Cargar la oportunidad con `Include(o => o.WaitlistEntry)` y comparar `opportunity.WaitlistEntry.BuyerEmail` con el email del request.
- **Rationale**: `FindByIdAsync` ya hace `Include(o => o.WaitlistEntry)` en el repositorio existente. No se necesita join adicional.
- **Alternatives considered**: Agregar `BuyerEmail` directamente a `WaitlistOpportunity` — rechazado porque viola normalización; el email ya está en `WaitlistEntry`.

### R3: ¿Cómo implementar el resultado tipado del handler de claim?

- **Decision**: Usar un enum `ClaimOpportunityResultType` con variantes `Claimed`, `NotFound`, `Expired`, `Forbidden` y un record `ClaimOpportunityResult`.
- **Rationale**: Sigue el patrón de `AssignOpportunityResult` y `ExpireOpportunityResult` ya existentes en el proyecto. El controller mapea cada variante al código HTTP correspondiente.
- **Alternatives considered**: Lanzar excepciones — rechazado porque las demas use cases del proyecto usan result types, no excepciones (excepto EnrollInWaitlist que es el más antiguo).

### R4: ¿La cuenta regresiva frontend debe recalcular desde expiresAt o decrementar remainingMinutes?

- **Decision**: Calcular localmente desde `expiresAt` con `setInterval(1000)`.
- **Rationale**: `remainingMinutes` del servidor solo es preciso al momento de la consulta. Para una cuenta regresiva fluida en MM:SS, el frontend calcula `differenceInSeconds(expiresAt, now)` cada segundo.
- **Alternatives considered**: Decrementar `remainingMinutes` cada 60s — rechazado porque la granularidad de un minuto es demasiado gruesa para una UX de urgencia.

### R5: ¿Cómo integrar WaitlistStatus en la página de compra?

- **Decision**: Después de una inscripción exitosa en WaitlistEnrollForm, mostrar automáticamente WaitlistStatus con el email ingresado. También ofrecer un campo de email para que compradores ya inscritos consulten directamente.
- **Rationale**: La página de compra ya usa renderizado condicional (disponibilidad → compra, sin disponibilidad → WaitlistEnrollForm). WaitlistStatus se integra como evolución de ese flujo.
- **Alternatives considered**: Crear una página separada `/waitlist/[eventId]` — rechazado porque fragmenta la experiencia del comprador y el ecosistema ya centra todo en `/buy/[id]`.

### R6: ¿Cómo redirigir al flujo de pago tras claim exitoso?

- **Decision**: Tras claim 200, usar el `ticketId` retornado para transicionar la página de compra al paso `reserved` con el ticket ya en estado reservado, alimentando directamente el `PaymentForm` existente.
- **Rationale**: El flujo de pago existente espera un `ticketId` reservado y un email. El claim ya garantiza que el ticket está reservado para el comprador. Se reutiliza el mismo mecanismo de la compra directa.
- **Alternatives considered**: Redireccionar a una URL externa — rechazado porque el PaymentForm es un componente inline en la misma página.
