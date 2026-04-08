# Implementation Plan: Notificación In-App de Lista de Espera

**Branch**: `004-inapp-notification` | **Date**: 2026-04-07 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-inapp-notification/spec.md`

## Summary

Cuando se activa o expira una oportunidad de lista de espera, el sistema emite un evento SSE en tiempo real al comprador conectado, sin requerir recarga de página. El endpoint SSE (`GET /api/waitlist/stream?email={email}`) mantiene conexiones long-lived filtradas por correo electrónico. Un consumer RabbitMQ dedicado escucha `waitlist.opportunity.activated` y despacha al hub SSE in-process, habilitando escalamiento horizontal futuro. La capa de notificación es exclusivamente de lectura: no modifica estado del dominio.

## Technical Context

**Language/Version**: C# / .NET 8  
**Primary Dependencies**: EF Core 8.0.4, Npgsql.EntityFrameworkCore.PostgreSQL 8.0.4, RabbitMQ.Client  
**Storage**: N/A — esta feature no persiste datos. Consume entidades existentes de solo lectura.  
**Testing**: xUnit 2.4.2, NSubstitute 5.1.0  
**Target Platform**: Linux server (Docker)  
**Project Type**: Endpoint SSE + consumer RabbitMQ dentro del CRUD Service (microservicio existente)  
**Performance Goals**: Notificación SSE recibida en < 2s desde la activación de la oportunidad (SC-001)  
**Constraints**: Keep-alive configurable (`SSE_KEEPALIVE_INTERVAL_SECONDS`, default 30s); máx. conexiones por email configurable (`SSE_MAX_CONNECTIONS_PER_EMAIL`, default 5)  
**Scale/Scope**: Conexiones SSE concurrentes proporcionales a inscripciones activas; volumen bajo para MVP

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Estado | Evidencia |
|-----------|--------|-----------|
| I. Hexagonal + DDD | ✅ PASS | Domain no se modifica. Application no se modifica. Infrastructure añade adaptadores SSE (`WaitlistSseHub`, `SseNotificationConsumer`). Api expone endpoint SSE en `WaitlistController`. Ningún framework se filtra hacia Domain. |
| II. Async-First | ✅ PASS | El evento viaja vía RabbitMQ (`waitlist.opportunity.activated`) → consumer → hub SSE → stream al cliente. Flujo 100% asíncrono. |
| III. TDD Strict | ✅ PASS | TC-HU4-01 a TC-HU4-03 definen la fase RED. Tests unitarios del observer/hub primero, luego implementación GREEN. |
| IV. Testing Pyramid | ✅ PASS | Unitarias (NSubstitute) para hub y consumer. Integración con `WebApplicationFactory` para endpoint SSE. |
| V. SOLID | ✅ PASS | SRP: hub gestiona conexiones, consumer despacha eventos, controller expone stream. OCP: hub reutilizable por futuros tipos de evento sin modificar controller. DIP: consumer depende de abstracción del hub (interfaz), no de implementación. ISP: interfaces separadas para notificar vs suscribir (patrón existente `ITicketStatusNotifier/Subscriber`). |
| VI. GoF-Only | ✅ PASS | Observer (IOpportunityObserver ya existe). No se agregan nuevos patrones GoF. |
| VII. Zero AI Smells | ✅ PASS | Nombres intencionales: `WaitlistSseHub`, `SseNotificationConsumer`, `WaitlistSseNotifier/Subscriber`. |
| VIII. Language Convention | ✅ PASS | Código en inglés, documentación en español. |

**GATE PASSED** — 8/8 principios cumplidos.

## Project Structure

### Documentation (this feature)

```text
specs/004-inapp-notification/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
crud_service/src/
├── CrudService.Domain/
│   └── Interfaces/
│       └── IOpportunityObserver.cs            # Ya existe — no se modifica
│
├── CrudService.Application/
│   └── (sin cambios para esta feature)
│
├── CrudService.Infrastructure/
│   ├── Sse/
│   │   ├── WaitlistSseHub.cs                  # NUEVO — ConcurrentDictionary<string, ConcurrentBag<SseClient>>
│   │   ├── IWaitlistSseNotifier.cs            # NUEVO — puerto ISP: SendEventAsync(email, eventType, payload)
│   │   └── IWaitlistSseSubscriber.cs          # NUEVO — puerto ISP: RegisterAsync/UnregisterAsync
│   ├── Messaging/
│   │   ├── SseNotificationConsumer.cs         # NUEVO — consumer RabbitMQ → hub SSE
│   │   └── OpportunityActivatedObserver.cs    # Ya existe — no se modifica
│   └── DependencyInjection.cs                 # Ya existe — extender con hub SSE singleton + ISP bindings
│
└── CrudService.Api/
    ├── Controllers/
    │   └── WaitlistController.cs              # Ya existe — agregar acción GET stream
    └── Program.cs                             # Ya existe — registrar SseNotificationConsumer como HostedService

crud_service/tests/
└── CrudService.Infrastructure.Tests/
    └── Sse/
        ├── WaitlistSseHubTests.cs             # NUEVO — tests unitarios del hub
        └── SseNotificationConsumerTests.cs    # NUEVO — tests unitarios del consumer
```

**Structure Decision**: Se reutiliza el patrón ISP existente de `TicketStatusHub` (`ITicketStatusNotifier/Subscriber`) adaptado para el dominio waitlist → `IWaitlistSseNotifier/IWaitlistSseSubscriber`. El hub vive en `Infrastructure/Sse/` como adaptador, no en Domain (no es lógica de negocio). El consumer RabbitMQ vive en `Infrastructure/Messaging/` junto al observer existente.

## Complexity Tracking

> No hay violaciones de Constitution Check que justificar.
