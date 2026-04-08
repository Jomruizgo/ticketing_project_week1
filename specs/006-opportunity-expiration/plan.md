# Implementation Plan: Expiración de Oportunidad de Lista de Espera

**Branch**: `006-opportunity-expiration` | **Date**: 2026-04-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/006-opportunity-expiration/spec.md`

## Summary

Implementar la expiración automática de oportunidades de lista de espera cuyo TTL ha vencido. Un consumer de RabbitMQ (`WaitlistOpportunityExpiredConsumer`) escucha la cola `q.waitlist.opportunity.expired` (mensajes ruteados por DLX desde `q.waitlist.opportunity.delay`). El handler `ExpireOpportunityHandler` transiciona la oportunidad de `active` a `expired`, intenta reasignación vía el flujo existente de HU3, y si no hay comprador elegible publica `ticket.returned_to_inventory` para devolver la entrada al inventario.

## Technical Context

**Language/Version**: C# / .NET 8  
**Primary Dependencies**: EF Core 8.0.4, Npgsql.EntityFrameworkCore.PostgreSQL 8.0.4, RabbitMQ.Client 6.8.1, EFCore.NamingConventions 8.0.1  
**Storage**: PostgreSQL (tabla `waitlist_opportunities` existente, columnas snake_case)  
**Testing**: xUnit 2.4.2, NSubstitute 5.1.0, Testcontainers.PostgreSql 3.9.0  
**Target Platform**: Linux server (Docker)  
**Project Type**: web-service (CRUD Service microservice)  
**Performance Goals**: Procesamiento de expiración en menos de 5 segundos desde la llegada del mensaje DLX  
**Constraints**: Idempotencia obligatoria; ACK en éxito/negocio, NACK con requeue:false en fallo técnico  
**Scale/Scope**: Afecta solo `crud_service/`, 6 artefactos nuevos + 3 modificaciones

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Estado | Notas |
|---|---|---|
| I. Hexagonal Architecture with DDD | ✅ PASS | Domain define puertos y entidades; Infrastructure implementa adaptadores (consumer RabbitMQ, repositorio EF Core). Application contiene el use case. La regla de dependencia se respeta. |
| II. Asynchronous-First Pipeline | ✅ PASS | El consumer escucha RabbitMQ asincrónicamente. No hay endpoints HTTP síncronos nuevos. La notificación SSE downstream ya existe vía `SseNotificationConsumer`. |
| III. TDD Strict | ✅ PASS | Tests RED antes de implementación. Commits `test(red):`, `feat(green):`, `refactor:`. |
| IV. Testing Pyramid | ✅ PASS | Unitarias para el handler (mocks de repos y observer). Integración con Testcontainers para verificar transición de estado en BD. |
| V. SOLID | ✅ PASS | SRP: handler solo expira y delega reasignación. OCP: observer extensible sin modificar handler. DIP: inyección por interfaces. ISP: `IExpireOpportunityUseCase` interfaz acotada. |
| VI. GoF-Only Design Patterns | ✅ PASS | Observer (notificación de expiración), State (transición Active→Expired), Command (ExpireOpportunityCommand), Strategy (reutiliza FIFO de HU3). |
| VII. Zero AI Smells | ✅ PASS | Nombres intencionales: `ExpireOpportunityHandler`, `WaitlistOpportunityExpiredConsumer`. Sin bloques catch vacíos — el consumer registra fallos. |
| VIII. Language Convention | ✅ PASS | Código en inglés, documentación en español, commits en inglés. |

**GATE RESULT: PASS — 8/8 principios cumplen.**

## Project Structure

### Documentation (this feature)

```text
specs/006-opportunity-expiration/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
crud_service/src/
├── CrudService.Domain/
│   ├── Interfaces/
│   │   ├── IWaitlistOpportunityRepository.cs  # MODIFY: agregar FindByIdAsync
│   │   └── IOpportunityObserver.cs            # MODIFY: agregar OnOpportunityExpiredAsync
│   └── Entities/
│       └── WaitlistOpportunity.cs             # MODIFY: agregar ExpiredAt, ExpirationReason
├── CrudService.Application/
│   └── UseCases/Waitlist/ExpireOpportunity/
│       ├── ExpireOpportunityCommand.cs        # NEW
│       ├── IExpireOpportunityUseCase.cs        # NEW
│       └── ExpireOpportunityHandler.cs         # NEW
├── CrudService.Infrastructure/
│   ├── Messaging/
│   │   ├── OpportunityActivatedObserver.cs    # MODIFY: implementar OnOpportunityExpiredAsync
│   │   └── WaitlistOpportunityExpiredConsumer.cs  # NEW
│   ├── Persistence/Repositories/
│   │   └── WaitlistOpportunityRepository.cs   # MODIFY: implementar FindByIdAsync
│   └── DependencyInjection.cs                 # MODIFY: registrar use case + HostedService
└── CrudService.Api/
    └── Program.cs                             # MODIFY: registrar consumer como HostedService

crud_service/tests/
├── CrudService.Application.Tests/
│   └── Waitlist/ExpireOpportunityHandlerTests.cs  # NEW
└── CrudService.Infrastructure.Tests/
    └── Integration/
        └── WaitlistOpportunityExpirationTests.cs  # NEW (Testcontainers)

scripts/
└── schema.sql                                 # MODIFY: agregar columnas expired_at, expiration_reason (si se decide)
```

**Structure Decision**: Se sigue la estructura hexagonal existente. El nuevo use case vive en `Application/UseCases/Waitlist/ExpireOpportunity/`. El consumer vive en `Infrastructure/Messaging/` junto a los consumers existentes (`TicketReleasedConsumer`, `SseNotificationConsumer`).

## Complexity Tracking

> Sin violaciones de constitución — no aplica.
