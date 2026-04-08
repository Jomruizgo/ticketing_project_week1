# Implementation Plan: Waitlist Opportunity Status & Claim UI

**Branch**: `008-waitlist-opportunity-ui` | **Date**: 2026-04-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/008-waitlist-opportunity-ui/spec.md`

## Summary

Implementar el caso de uso de reclamación de oportunidad (ClaimOpportunity) en el CRUD Service backend y la UI frontend de consulta de estado, acción sobre oportunidad y actualizaciones en tiempo real vía SSE. El backend agrega el endpoint POST /api/waitlist/opportunities/{id}/claim al WaitlistController existente, delegando al nuevo IClaimOpportunityUseCase. El frontend agrega tipos, funciones API, un hook SSE, un componente de estado y su integración en la página de compra.

## Technical Context

**Language/Version**: C# / .NET 8 (backend), TypeScript 5.7.3 (frontend)
**Primary Dependencies**: EF Core 8.0.4 + Npgsql (backend), React 19.2.3 + Next.js 16.1.6 + shadcn/ui + sonner (frontend)
**Storage**: PostgreSQL — tablas `waitlist_entries`, `waitlist_opportunities` (existentes)
**Testing**: xUnit + Moq (backend unit), Testcontainers (backend integration), vitest + @testing-library/react (frontend unit)
**Target Platform**: Navegadores web modernos + servidor Linux
**Project Type**: Sistema distribuido — CRUD Service (C#) + frontend SPA (Next.js)
**Performance Goals**: Consulta de estado < 3s, transición SSE < 2s, flujo de claim < 5s (SC-001 a SC-003)
**Constraints**: No modificar tablas de BD ni esquema existente. El dominio WaitlistOpportunity ya soporta la transición Active→Consumed via TransitionTo()
**Scale/Scope**: 1 use case backend nuevo + 1 endpoint nuevo + 2 tipos frontend nuevos + 2 funciones API frontend nuevas + 1 hook SSE + 1 componente nuevo + 1 modificación de página

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Aplica | Estado | Notas |
|-----------|--------|--------|-------|
| I. Hexagonal Architecture with DDD | Sí | ✅ PASS | Backend: ClaimOpportunityHandler en Application, IClaimOpportunityUseCase como puerto, WaitlistController en Api. La transición de estado usa WaitlistOpportunity.TransitionTo() del Domain. Frontend: api.ts como adaptador, componente como UI |
| II. Asynchronous-First Pipeline | Parcialmente | ✅ PASS | El claim es síncrono (200 directo) porque es una operación CRUD del crud_service: cambia estado Active→Consumed y retorna la respuesta. El SSE es inherentemente asíncrono. Consistente con GET /api/waitlist/entries (también síncrono) |
| III. TDD Strict | Sí | ✅ PASS | Backend: tests unitarios del handler (RED) antes de implementar. Frontend: tests de componente y API (RED) antes de implementar. Commits: test(red) → feat(green) → refactor |
| IV. Testing Pyramid | Sí | ✅ PASS | Backend unitarias (handler mock) + integración (Testcontainers). Frontend unitarias (@testing-library/react) + aceptación manual (TC-HU8-01 a TC-HU8-04) |
| V. SOLID | Sí | ✅ PASS | SRP: ClaimOpportunityHandler tiene una sola responsabilidad. DIP: controller depende del puerto IClaimOpportunityUseCase. Frontend: WaitlistStatus depende de api abstraction |
| VI. GoF-Only Design Patterns | Sí | ✅ PASS | State pattern ya implementado en WaitlistOpportunity.TransitionTo() (Active→Consumed). Observer pattern en IOpportunityObserver. No se agregan patrones nuevos |
| VII. Code Quality: Zero AI Smells | Sí | ✅ PASS | Nombres descriptivos, sin code muerto |
| VIII. Language Convention | Sí | ✅ PASS | Código en inglés, documentación en español, commits en inglés |

**Gate result**: ✅ PASS

## Project Structure

### Documentation (this feature)

```text
specs/008-waitlist-opportunity-ui/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
# Backend (CRUD Service)
crud_service/
├── src/
│   ├── CrudService.Api/
│   │   └── Controllers/
│   │       └── WaitlistController.cs           # MODIFICAR: agregar endpoint POST opportunities/{id}/claim
│   ├── CrudService.Application/
│   │   ├── Dtos/
│   │   │   └── WaitlistDtos.cs                 # MODIFICAR: agregar ClaimOpportunityRequest, ClaimOpportunityResponse
│   │   └── UseCases/
│   │       └── Waitlist/
│   │           └── ClaimOpportunity/           # NUEVO: directorio
│   │               ├── IClaimOpportunityUseCase.cs  # NUEVO: puerto
│   │               ├── ClaimOpportunityCommand.cs   # NUEVO: command
│   │               └── ClaimOpportunityHandler.cs   # NUEVO: handler
│   ├── CrudService.Domain/
│   │   └── (sin cambios — TransitionTo Active→Consumed ya existe)
│   └── CrudService.Infrastructure/
│       └── DependencyInjection.cs              # MODIFICAR: registrar ClaimOpportunityHandler
└── tests/
    └── CrudService.Application.Tests/
        └── Waitlist/
            └── ClaimOpportunityHandlerTests.cs  # NUEVO: tests unitarios

# Frontend
frontend/
├── app/
│   └── buy/
│       └── [id]/
│           └── page.tsx                         # MODIFICAR: integrar WaitlistStatus
├── components/
│   └── waitlist-status.tsx                      # NUEVO: componente de estado + claim + countdown
├── hooks/
│   └── use-waitlist-sse.ts                      # NUEVO: hook SSE
├── lib/
│   ├── api.ts                                   # MODIFICAR: agregar getWaitlistStatus(), claimOpportunity()
│   └── types.ts                                 # MODIFICAR: agregar WaitlistStatusResponse, WaitlistOpportunityDto
└── tests/
    ├── components/
    │   └── waitlist-status.test.tsx              # NUEVO: tests del componente
    └── lib/
        └── api-waitlist-status.test.ts          # NUEVO: tests de funciones API
```

**Structure Decision**: Backend sigue la estructura hexagonal existente del CRUD Service. Frontend sigue la convención establecida en 007-waitlist-enrollment-ui. No se crean proyectos nuevos.

## Complexity Tracking

> Sin violaciones de constitución que justificar.
