# Research — 005-email-notification

**Date**: 2026-04-08

## R1: Ciclo de vida del status en notification_deliveries

**Decision**: Tres estados (`pending`, `sent`, `failed`) con exactamente una transición permitida: `pending → sent` o `pending → failed`.

**Rationale**: El esquema de BD (`bd_esquema.drawio`) ya define estos tres estados. El estado `pending` permite detectar intentos incompletos (crash entre creación del registro y respuesta del proveedor). La inmutabilidad se aplica solo tras estado terminal, no como append-only puro (sobre-ingeniería para una tabla operativa interna).

**Alternatives considered**:
- Append-only puro (crear directamente como `sent`/`failed`): pierde trazabilidad de crashes.
- Doble registro (un `pending` + un `sent`/`failed`): duplica datos sin beneficio.

## R2: Obtención del nombre del evento para el correo

**Decision**: Enriquecer el payload de `IOpportunityObserver.OnOpportunityActivatedAsync` con un record tipado `OpportunityActivatedEvent` (definido en `Domain/Events/`) que incluye `EventName` ya resuelto upstream por el handler de asignación.

**Rationale**: Tell Don't Ask (GoF Observer canónico). El handler ya tiene acceso a `IEventRepository` y puede resolver `eventId → eventName` una sola vez. Empujar datos downstream evita N queries redundantes si hay N observers. Mejora testabilidad (construir un record es trivial vs. mockear un `IEventQueryPort`).

**Alternatives considered**:
- Inyectar `IEventQueryPort` en el observer: queries redundantes por observer, peor testabilidad.
- Pasar la entidad `WaitlistOpportunity` + load eager de `Event`: acopla interfaz de notificación al modelo de dominio.

## R3: Refactor de IOpportunityObserver (singular → IEnumerable)

**Decision**: Refactorear `AssignOpportunityHandler` para inyectar `IEnumerable<IOpportunityObserver>` e iterar con `foreach`. Registrar múltiples `AddScoped<IOpportunityObserver, ...>()` en DI. MS DI soporta esto nativamente.

**Rationale**: El handler actual inyecta un solo `IOpportunityObserver`. Para agregar `EmailNotificationObserver` como segundo observer sin violar OCP, se necesita la colección. Este es el patrón documentado en `patterns/observer.md` (que ya muestra `IEnumerable`) pero no implementado aún.

**Alternatives considered**:
- Composite Observer: wrappear múltiples observers en uno solo. Agrega complejidad innecesaria — el foreach es suficiente.
- Mediator/pub-sub interno: sobre-ingeniería para 2 observers.

## R4: Patrón de registro de enums PostgreSQL

**Decision**: Replicar el patrón existente: enum C# con `[PgName("minúscula")]` en Domain/Enums, `HasPostgresEnum<>()` en `OnModelCreating`, `HasColumnType("nombre_tipo_pg")` en la configuración de la entidad.

**Rationale**: Consistencia con `WaitlistOpportunityStatus`, `WaitlistEntryStatus` y los otros enums del proyecto.

**Alternatives considered**: Ninguna — el patrón es establecido y funcional.

## R5: Proveedor de correo para MVP

**Decision**: Implementar `LogEmailSender` como adaptador stub que solo loguea el intento (no envía correo real). Implementa `IEmailSender` retornando `EmailSendResult(true, null)` siempre. Permite desarrollo TDD completo sin infraestructura externa de correo.

**Rationale**: El MVP prioriza la auditoría y el aislamiento. El proveedor real (SES, SendGrid, SMTP) se conecta después cambiando solo el registro DI — la interfaz `IEmailSender` garantiza el desacople.

**Alternatives considered**:
- MailHog/Papercut (servidor SMTP local para testing): agrega dependencia de infraestructura al compose sin valor para MVP.
- Implementación directa con SMTP: requiere configuración de servidor real, no necesaria en esta fase.

## R6: Ubicación del record OpportunityActivatedEvent

**Decision**: `Domain/Events/OpportunityActivatedEvent.cs` — directorio nuevo en Domain.

**Rationale**: El record es un objeto de valor del dominio que describe un evento de negocio. No es un DTO de infraestructura. Definirlo en Domain permite que tanto Application (handler) como los puertos (IOpportunityObserver en Domain) lo referencien sin violar la regla de dependencia.

**Alternatives considered**:
- Dejar `OpportunityActivatedEvent` en Infrastructure/Messaging: viola la regla de dependencia (Domain/Interfaces/IOpportunityObserver no puede referenciar Infrastructure).
- Application/DTOs: semánticamente incorrecto — es un evento de dominio, no un DTO de capa de aplicación.

## R7: DDL para notification_deliveries

**Decision**: Agregar a `scripts/schema.sql`:
1. `CREATE TYPE notification_delivery_status AS ENUM ('pending', 'sent', 'failed');`
2. `CREATE TABLE notification_deliveries (...)` con FK a `waitlist_opportunities`.
3. Índice por `waitlist_opportunity_id` para consultas de soporte (SC-006).

**Rationale**: Replicar el patrón SQL existente del proyecto. La tabla no existe actualmente en schema.sql.

**Alternatives considered**: EF Migrations — el proyecto no las usa; todas las tablas se definen en schema.sql.
