# Implementation Plan: Consulta de Estado de Lista de Espera

**Branch**: `002-waitlist-status-query` | **Date**: 2026-04-07 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-waitlist-status-query/spec.md` + user arguments (plan prompt)

## Summary

Implementar un endpoint de consulta `GET /api/waitlist/entries?eventId={id}&email={email}` en el CrudService que permite a un comprador verificar su estado actual en la lista de espera. El handler consulta la inscripción activa más reciente del comprador y, si existe, cualquier oportunidad de compra vinculada. Devuelve un DTO que proyecta los cuatro estados visibles: inscripción activa (en espera), oportunidad activa (con tiempo restante), oportunidad consumida, oportunidad expirada. La entidad `WaitlistOpportunity` se define como puerto/interfaz; si no existe aún en BD, la consulta simplemente devuelve oportunidad nula. La feature es de solo lectura.

## Technical Context

**Language/Version**: C# / .NET 8  
**Primary Dependencies**: EF Core 8.0.4, Npgsql.EntityFrameworkCore.PostgreSQL 8.0.4, EFCore.NamingConventions 8.0.1  
**Storage**: PostgreSQL — tablas `waitlist_entries` (existente), `waitlist_opportunities` (futura; se define el modelo pero puede no existir en BD)  
**Testing**: xUnit + Moq (unitarias Application), Testcontainers (integración Infrastructure si aplica)  
**Target Platform**: Linux (Docker) / CrudService API  
**Project Type**: Microservicio web (API REST síncrona)  
**Performance Goals**: Respuesta < 2 segundos (SC-001)  
**Constraints**: Solo lectura — cero efectos secundarios (FR-011, SC-005)  
**Scale/Scope**: Feature acotada; 1 endpoint, 1 caso de uso, 2 repositorios

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Estado | Notas |
|-----------|--------|-------|
| I. Hexagonal Architecture with DDD | ✅ PASS | Query + Handler en Application, IWaitlistOpportunityRepository como puerto en Domain, adaptador EF Core en Infrastructure, GET en WaitlistController en Api |
| II. Asynchronous-First Pipeline | ✅ N/A | Esta feature es consulta síncrona al CrudService — no involucra Producer ni Workers. Consistente con el rol del CrudService como servicio de lectura/escritura síncrono |
| III. TDD Strict | ✅ PASS | Se definen 5 tests unitarios en Application.Tests para el handler: 4 estados visibles + inscripción no encontrada. Ciclo RED → GREEN → REFACTOR obligatorio |
| IV. Testing Pyramid | ✅ PASS | Unitarias (base): handler con mocks. No se requieren pruebas de integración con Testcontainers porque la feature es de solo lectura simple y el repositorio no tiene lógica compleja de BD |
| V. SOLID | ✅ PASS | SRP: handler solo proyecta estado. OCP: extensible si se agregan nuevos estados. ISP: IGetWaitlistStatusUseCase interfaz segregada. DIP: handler depende solo de abstracciones |
| VI. GoF-Only Design Patterns | ✅ PASS | No se introducen patrones nuevos. El Command pattern del handler existente se reutiliza como Query |
| VII. Code Quality: Zero AI Smells | ✅ PASS | Nombres descriptivos, sin código muerto, sin duplicación |
| VIII. Language Convention | ✅ PASS | Código en inglés, documentación en español |

**Gate Result**: ✅ Todos los principios aprobados — proceder a Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/002-waitlist-status-query/
├── spec.md              # Especificación de la feature
├── plan.md              # Este archivo
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── api.md           # Contrato del endpoint GET
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
crud_service/src/
├── CrudService.Domain/
│   ├── Entities/
│   │   ├── WaitlistEntry.cs          # [001] existente — sin cambios
│   │   └── WaitlistOpportunity.cs    # [002] nueva entidad de dominio
│   ├── Enums/
│   │   ├── WaitlistEntryStatus.cs    # [001] existente — sin cambios
│   │   └── WaitlistOpportunityStatus.cs  # [002] nuevo enum
│   └── Interfaces/
│       ├── IWaitlistEntryRepository.cs   # [001] existente — se agrega FindActiveByEventAndEmailAsync
│       └── IWaitlistOpportunityRepository.cs  # [002] nuevo puerto
├── CrudService.Application/
│   ├── Dtos/
│   │   └── WaitlistDtos.cs           # [001] existente — se agregan WaitlistStatusResponse y WaitlistOpportunityDto
│   └── UseCases/
│       └── Waitlist/
│           ├── EnrollInWaitlist/      # [001] existente — sin cambios
│           └── GetWaitlistStatus/     # [002] nueva carpeta
│               ├── GetWaitlistStatusQuery.cs
│               ├── GetWaitlistStatusHandler.cs
│               └── IGetWaitlistStatusUseCase.cs
├── CrudService.Infrastructure/
│   ├── DependencyInjection.cs        # [001] existente — se agregan registros 002
│   └── Persistence/
│       ├── TicketingDbContext.cs      # [001] existente — se agrega DbSet<WaitlistOpportunity> + config
│       └── Repositories/
│           ├── WaitlistEntryRepository.cs      # [001] existente — se agrega FindActiveByEventAndEmailAsync
│           └── WaitlistOpportunityRepository.cs  # [002] nuevo adaptador
└── CrudService.Api/
    └── Controllers/
        └── WaitlistController.cs     # [001] existente — se agrega acción GET

crud_service/tests/
├── CrudService.Application.Tests/
│   └── Waitlist/
│       ├── EnrollInWaitlistHandlerTests.cs   # [001] existente — sin cambios
│       └── GetWaitlistStatusHandlerTests.cs  # [002] nuevo
```

**Structure Decision**: Se reutiliza la estructura existente de 001 (arquitectura hexagonal del CrudService). Todos los artefactos nuevos se agregan en las carpetas correspondientes. No se crean nuevos proyectos .csproj ni capas adicionales.

## Complexity Tracking

> Sin violaciones de constitución que justificar.
