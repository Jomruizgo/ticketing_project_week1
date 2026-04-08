# Research: Expiración de Oportunidad de Lista de Espera

**Feature**: 006-opportunity-expiration  
**Date**: 2026-04-08

## R1: Agregar `FindByIdAsync` a `IWaitlistOpportunityRepository`

**Decision**: Agregar `Task<WaitlistOpportunity?> FindByIdAsync(long id)` al puerto y su implementación con `_context.WaitlistOpportunities.Include(o => o.WaitlistEntry).FirstOrDefaultAsync(o => o.Id == id)`.

**Rationale**: El consumer recibe solo `opportunityId` del mensaje DLX. Los métodos existentes (`FindByWaitlistEntryIdAsync`, `FindActiveByTicketIdAsync`) no permiten buscar por ID de oportunidad. Incluir navegación a `WaitlistEntry` porque el handler necesita `WaitlistEntry.EventId` para la reasignación y `WaitlistEntry.BuyerEmail` para el observer.

**Alternatives considered**: (1) Añadir un DTO intermedio con eventId + ticketId en el mensaje DLX para evitar el query — rechazado porque complica el payload y requiere mantener consistencia entre el mensaje y la BD. (2) Buscar por ticketId con `FindActiveByTicketIdAsync` — rechazado porque después de expirar ya no es `active`, introduce ambigüedad y el método filtra por estado.

## R2: Agregar `OnOpportunityExpiredAsync` a `IOpportunityObserver`

**Decision**: Extender la interfaz con `Task OnOpportunityExpiredAsync(WaitlistOpportunity opportunity)`. Implementar en `OpportunityActivatedObserver` para publicar a RabbitMQ con routing key `waitlist.opportunity.expired` y exchange `tickets`.

**Rationale**: La interfaz actual solo tiene `OnOpportunityActivatedAsync`. La expiración necesita notificar a los observers para que `SseNotificationConsumer` (que ya escucha la routing key `waitlist.opportunity.expired`) emita el evento SSE `opportunity_expired` al comprador. Seguir el principio OCP: extender la interfaz sin modificar los consumidores existentes.

**Alternatives considered**: (1) Publicar directamente a RabbitMQ desde el handler sin pasar por el observer — rechazado porque viola SRP y la regla de dependencia (Application no debe conocer RabbitMQ). (2) Crear un segundo observer separado para expiración — rechazado por ISP: un solo observer con ambos métodos es suficiente mientras el handler solo necesite notificar un cambio de estado.

**Nota**: Si HU5 ya refactoreó `AssignOpportunityHandler` a `IEnumerable<IOpportunityObserver>`, `ExpireOpportunityHandler` debe inyectar de la misma forma. Si no, inyectar singular `IOpportunityObserver` es suficiente porque `OpportunityActivatedObserver` implementa ambos métodos.

## R3: Campos `ExpiredAt` / `ExpirationReason` en `WaitlistOpportunity`

**Decision**: NO agregar columnas a la BD ni a la entidad. Usar `ExpiresAt` (ya existente) como referencia de cuándo debió expirar. El motivo es siempre `ttl_expired` (único mecanismo de expiración). Registrar el evento de expiración en logs estructurados.

**Rationale**: La spec dice "registrar motivo y marca de tiempo de expiración". Esto puede cumplirse con: (1) `ExpiresAt` ya da la marca temporal planificada, y el `UpdatedAt` / log del evento cubre cuándo se procesó. (2) El motivo es siempre el mismo (`ttl_expired`) porque no hay expiración manual ni por otro trigger. Agregar columnas a la BD y ALTER TABLE en schema.sql agrega complejidad sin valor diferencial para el MVP.

**Alternatives considered**: (1) Agregar `expired_at TIMESTAMPTZ` y `expiration_reason VARCHAR(100)` a `waitlist_opportunities` — viable para auditoría detallada, pero overengineering para MVP con un solo mecanismo de expiración. Puede agregarse en una iteración futura si el negocio requiere múltiples tipos de expiración.

**Trade-off aceptado**: Si en el futuro se necesita expiración manual o por admin, deberá agregarse. Para MVP con solo DLX, los logs son suficientes.

## R4: Patrón de reasignación post-expiración

**Decision**: Reutilizar `IAssignOpportunityUseCase.HandleAsync(new AssignOpportunityCommand(ticketId, eventId))` directamente desde el handler de expiración. Si retorna `NoEligible` o `AllFailed`, publicar `ticket.returned_to_inventory`.

**Rationale**: El flujo de asignación (FIFO, reserva temporal, creación de oportunidad) ya está probado en HU3. Duplicar la lógica viola DRY. `TicketReleasedConsumer` ya implementa el mismo patrón (invocar asignación → si no hay elegible → devolver al inventario), lo que valida el approach.

**Alternatives considered**: (1) Emitir un evento y que `TicketReleasedConsumer` lo procese — rechazado porque usar `ticket.released` crearía un loop (el consumer de released volvería a asignar, que volvería a expirar, etc.). Usar `ticket.returned_to_inventory` como routing key es correcto para el retorno al inventario, pero la reasignación debe ocurrir in-process antes de decidir si devolver.

## R5: Consumer `WaitlistOpportunityExpiredConsumer`

**Decision**: Implementar como `BackgroundService` siguiendo el patrón de `TicketReleasedConsumer`. Escuchar `q.waitlist.opportunity.expired`, deserializar `{opportunityId}`, crear scope DI, invocar `IExpireOpportunityUseCase.HandleAsync`.

**Rationale**: El patrón ya existe y funciona: `TicketReleasedConsumer` es un `BackgroundService` con retry de conexión, `BasicQos(0, 1, false)`, ACK/NACK. Replicar el mismo approach asegura consistencia operativa. Crear un scope por mensaje es necesario porque los servicios scoped (repos, handler) no pueden inyectarse en un singleton.

**Alternatives considered**: (1) MassTransit u otro framework de consumidores — rechazado porque el proyecto usa RabbitMQ.Client 6.8.1 directamente, sin abstracción adicional. Agregar MassTransit sería un cambio arquitectural fuera de alcance.

## R6: Publicación de `ticket.returned_to_inventory`

**Decision**: Publicar desde el handler (vía un puerto `IInventoryReturnPort` en Domain, implementado como adaptador RabbitMQ en Infrastructure) cuando `IAssignOpportunityUseCase` retorna `NoEligible` o `AllFailed`.

**Rationale**: El handler necesita decidir si devolver al inventario después de intentar reasignación. `TicketReleasedConsumer` ya publica este evento con el mismo payload `{ticketId, eventId, returnedAt}`. Para evitar duplicar la lógica de publicación RabbitMQ en Application (violando la regla de dependencia), definir un puerto en Domain e implementar el adaptador en Infrastructure.

**Alternatives considered**: (1) Publicar directamente con `RabbitMQ.Client` desde el handler — rechazado porque viola arquitectura hexagonal. (2) Reutilizar la publicación que ya hace `TicketReleasedConsumer` — no reutilizable porque es código dentro de un `BackgroundService`, no un servicio inyectable. Extraer a un adaptador compartido es la solución correcta.
