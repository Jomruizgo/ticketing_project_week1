# Implementation Plan: Notificación por Correo Electrónico de Oportunidad de Lista de Espera

**Branch**: `005-email-notification` | **Date**: 2026-04-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-email-notification/spec.md`

## Summary

Implementar un observer in-process (`EmailNotificationObserver`) que envía un correo electrónico al comprador cuando su oportunidad de lista de espera se activa. El correo es informativo (no es fuente oficial del estado). Cada intento queda auditado en `notification_deliveries` con ciclo de vida `pending → sent | failed`. El canal está completamente aislado del ciclo de vida de la oportunidad: un fallo de correo nunca afecta la oportunidad. Requiere refactor previo de `IOpportunityObserver` para recibir un record tipado enriquecido (`OpportunityActivatedEvent` en Domain) y del handler para inyectar `IEnumerable<IOpportunityObserver>`.

## Technical Context

**Language/Version**: C# / .NET 8  
**Primary Dependencies**: EF Core 8.0.4, Npgsql.EntityFrameworkCore.PostgreSQL 8.0.4, EFCore.NamingConventions 8.0.1, RabbitMQ.Client 6.8.1, Microsoft.Extensions.Logging.Abstractions 8.0.0  
**Storage**: PostgreSQL (tabla `notification_deliveries`, snake_case automático via NamingConventions)  
**Testing**: xUnit 2.4.2, NSubstitute 5.1.0, Testcontainers (integración)  
**Target Platform**: Linux container (Docker Compose)  
**Project Type**: Web service (CRUD Service — API + observers in-process)  
**Performance Goals**: El timeout del proveedor de correo no debe incrementar el tiempo de asignación más de 5 segundos (SC-005)  
**Constraints**: Timeout configurable via variable de entorno. Sin reintentos automáticos. Texto plano para MVP  
**Scale/Scope**: Tabla append-con-update (inmutable tras terminal). N observers in-process. Un solo proveedor de correo abstráido

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Cumple | Notas |
|-----------|--------|-------|
| I. Hexagonal + DDD | ✅ | `OpportunityActivatedEvent` record en Domain. `IEmailSender`, `INotificationDeliveryRepository` como puertos en Domain/Interfaces. Adaptadores en Infrastructure. ⚠️ El código actual viola este principio (Npgsql en Domain, interfaces de Infrastructure en Api). Phase 0 de tasks.md (T-ARCH-01..06) remedia estas violaciones preexistentes. Tras ejecutar Phase 0, Domain no referencia frameworks |
| II. Async-First | ✅ | El observer es in-process (sin latencia de cola), pero el correo en sí es un efecto secundario asíncrono desde la perspectiva del comprador. No cambia el flujo 202 del Producer |
| III. TDD Strict | ✅ | TC-HU5-01 a TC-HU5-04 definidos. RED → GREEN → REFACTOR obligatorio |
| IV. Testing Pyramid | ✅ | Unitarias (observer + aislamiento) + Integración con Testcontainers (notification_deliveries) |
| V. SOLID | ✅ | SRP (observer solo notifica), OCP (nuevo observer sin tocar handler), LSP (EmailNotificationObserver sustituible por IOpportunityObserver), ISP (IEmailSender mínimo), DIP (puertos en Domain) |
| VI. GoF-Only | ✅ | Observer (GoF) documentado en patterns/observer.md |
| VII. Zero AI Smells | ✅ | Nombres intencionales, sin código muerto, sin try-catch vacíos (se registra en notification_deliveries) |
| VIII. Language Convention | ✅ | Código en inglés, documentación en español, commits en inglés |

**Gate result: PASS** — Sin violaciones.

## Project Structure

### Documentation (this feature)

```text
specs/005-email-notification/
├── spec.md              # Especificación (ya existe)
├── plan.md              # Este archivo
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # N/A — sin API endpoints nuevos
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
crud_service/src/
├── CrudService.Domain/
│   ├── Entities/
│   │   └── NotificationDelivery.cs          # NUEVA entidad
│   ├── Enums/
│   │   └── NotificationDeliveryStatus.cs    # NUEVO enum (Pending, Sent, Failed)
│   ├── Events/
│   │   └── OpportunityActivatedEvent.cs     # NUEVO record tipado (Domain)
│   └── Interfaces/
│       ├── IOpportunityObserver.cs           # MODIFICAR firma
│       ├── IEmailSender.cs                   # NUEVO puerto + record EmailSendResult
│       └── INotificationDeliveryRepository.cs # NUEVO puerto
├── CrudService.Application/
│   └── UseCases/Waitlist/AssignOpportunity/
│       └── AssignOpportunityHandler.cs       # MODIFICAR: IEnumerable + record
├── CrudService.Infrastructure/
│   ├── Messaging/
│   │   ├── OpportunityActivatedObserver.cs   # MODIFICAR: nueva firma
│   │   └── OpportunityActivatedEvent.cs      # ELIMINAR (movido a Domain/Events)
│   ├── Persistence/
│   │   ├── TicketingDbContext.cs              # MODIFICAR: agregar DbSet + enum
│   │   └── Repositories/
│   │       └── NotificationDeliveryRepository.cs # NUEVO adaptador
│   ├── Services/
│   │   ├── EmailNotificationObserver.cs      # NUEVO observer
│   │   └── LogEmailSender.cs                 # NUEVO adaptador stub (log-only MVP)
│   └── DependencyInjection.cs                # MODIFICAR: registros DI

crud_service/tests/
├── CrudService.Application.Tests/
│   └── Waitlist/
│       └── AssignOpportunityHandlerTests.cs   # MODIFICAR: IEnumerable + record
└── CrudService.Infrastructure.Tests/           # Ya existe, agregar:
    └── Waitlist/
        └── EmailNotificationObserverTests.cs  # NUEVO (TC-HU5-01..03)

scripts/
└── schema.sql                                 # MODIFICAR: agregar notification_deliveries
```

**Structure Decision**: Se reutiliza la estructura hexagonal existente del CRUD Service. La entidad `NotificationDelivery` y el record `OpportunityActivatedEvent` van en Domain (capa interna). El observer y el sender van en Infrastructure (adaptadores). No se crea un proyecto nuevo — la feature es un adaptador adicional dentro del bounded context de lista de espera.

## Complexity Tracking

No hay violaciones de constitución que justificar.
