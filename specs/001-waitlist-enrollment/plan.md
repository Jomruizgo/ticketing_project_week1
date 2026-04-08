# Implementation Plan: Inscripción en Lista de Espera

**Branch**: `001-waitlist-enrollment` | **Date**: 2026-04-07 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/001-waitlist-enrollment/spec.md`

## Summary

Agregar la capacidad de inscripción en lista de espera al CRUD Service existente. Un comprador se registra con su correo electrónico para un evento; el sistema crea una `WaitlistEntry` con estado `active`. Se aplica unicidad por par (evento, correo) con partial unique index en PostgreSQL y validación en la capa de aplicación. La lista cierra cuando la fecha del evento (`StartsAt`) se alcanza. La feature sigue la arquitectura hexagonal existente: entidad y puerto en Domain, comando + handler en Application, repositorio EF Core en Infrastructure, controller en Api.

## Technical Context

**Language/Version**: C# / .NET 8  
**Primary Dependencies**: EF Core 8.0.4, Npgsql.EntityFrameworkCore.PostgreSQL 8.0.4, EFCore.NamingConventions 8.0.1  
**Storage**: PostgreSQL (tabla `waitlist_entries`, snake_case automático)  
**Testing**: xUnit + NSubstitute (unitarias), Testcontainers para PostgreSQL (integración)  
**Target Platform**: Linux container (Docker Compose)  
**Project Type**: Web service (API REST síncrona dentro del CRUD Service)  
**Performance Goals**: < 5 segundos por inscripción (SC-001 de la spec)  
**Constraints**: Partial unique index en BD para concurrencia; validación doble (aplicación + BD)  
**Scale/Scope**: Feature aditiva en un microservicio existente; 1 entidad nueva, 1 endpoint nuevo

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Estado | Evidencia |
|-----------|--------|-----------|
| I. Hexagonal Architecture with DDD | ✅ PASS | `WaitlistEntry` en Domain, `IWaitlistEntryRepository` como puerto, handler en Application, repositorio EF Core en Infrastructure, controller en Api. Domain no referencia frameworks. |
| II. Asynchronous-First Pipeline | ✅ PASS (N/A) | Esta feature es síncrona por diseño: el CRUD Service gestiona lecturas/escrituras síncronas (según constitución). No involucra RabbitMQ ni el pipeline asíncrono. |
| III. TDD Strict | ✅ PASS | Plan especifica pruebas RED antes de implementación. Casos de test definidos: TC-HU1-01 a TC-HU1-05. Commits seguirán convención `test(red):` / `feat(green):` / `refactor:`. |
| IV. Testing Pyramid | ✅ PASS | Unitarias con NSubstitute para handler (Application), integración con Testcontainers para repositorio y partial unique index (Infrastructure). |
| V. SOLID | ✅ PASS | SRP: handler tiene una sola responsabilidad (inscribir). ISP: `IWaitlistEntryRepository` expone solo lo necesario. DIP: handler depende de abstracciones (interfaces de Domain). |
| VI. GoF-Only Design Patterns | ✅ PASS | No se introducen patrones nuevos. El patrón Command existente (Command + Handler) se reutiliza. |
| VII. Code Quality: Zero AI Smells | ✅ PASS | Nombres intencionales: `EnrollInWaitlistCommand`, `WaitlistEntry`, `BuyerEmail`. Sin genéricos. |
| VIII. Language Convention | ✅ PASS | Código en inglés, documentación en español. |

**Resultado**: Todos los gates pasan. Proceder a Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/001-waitlist-enrollment/
├── plan.md              # Este archivo
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── waitlist-api.md
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
crud_service/
├── src/
│   ├── CrudService.Domain/
│   │   ├── Entities/
│   │   │   └── WaitlistEntry.cs              # Nueva entidad
│   │   ├── Enums/
│   │   │   └── WaitlistEntryStatus.cs        # Nuevo enum
│   │   ├── Exceptions/
│   │   │   ├── DuplicateWaitlistEntryException.cs   # Nueva excepción
│   │   │   └── WaitlistClosedException.cs           # Nueva excepción
│   │   └── Interfaces/
│   │       └── IWaitlistEntryRepository.cs   # Nuevo puerto
│   ├── CrudService.Application/
│   │   ├── Dtos/
│   │   │   └── WaitlistDtos.cs               # Nuevos DTOs
│   │   └── UseCases/
│   │       └── Waitlist/
│   │           └── EnrollInWaitlist/
│   │               ├── EnrollInWaitlistCommand.cs
│   │               ├── EnrollInWaitlistHandler.cs
│   │               └── IEnrollInWaitlistUseCase.cs
│   ├── CrudService.Infrastructure/
│   │   ├── DependencyInjection.cs            # Modificar (agregar registros)
│   │   └── Persistence/
│   │       ├── TicketingDbContext.cs          # Modificar (agregar DbSet + config)
│   │       └── Repositories/
│   │           └── WaitlistEntryRepository.cs # Nuevo repositorio
│   └── CrudService.Api/
│       ├── Controllers/
│       │   └── WaitlistController.cs         # Nuevo controller
│       └── Program.cs                        # Modificar (mapear enum)
├── tests/
│   ├── CrudService.Application.Tests/
│   │   └── Waitlist/
│   │       └── EnrollInWaitlistHandlerTests.cs  # TC-HU1-01..04
│   └── CrudService.Infrastructure.Tests/
│       └── Integration/
│           └── WaitlistEntryRepositoryTests.cs  # TC-HU1-05
└── scripts/
    └── schema.sql                            # Modificar (agregar tabla + index)
```

**Structure Decision**: Reutilizar la estructura hexagonal existente del CRUD Service. Los archivos nuevos siguen las mismas convenciones de carpetas y namespaces que las entidades/repositorios/handlers existentes (Event, Ticket, Payment). No se crea un proyecto nuevo.

## Complexity Tracking

> Sin violaciones de constitución. No aplica.
