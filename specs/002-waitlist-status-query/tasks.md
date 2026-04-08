# Tasks: Consulta de Estado de Lista de Espera

**Input**: Design documents from `/specs/002-waitlist-status-query/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/api.md ✅, quickstart.md ✅

**Tests**: Incluidos — TDD estricto requerido por constitución y plan prompt.

**Organization**: Tasks agrupadas por user story para implementación y prueba independiente.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivos distintos, sin dependencias) 
- **[Story]**: User story a la que pertenece (US1, US2, US3, US4)
- Rutas exactas relativas a la raíz del repositorio

---

## Phase 1: Setup

**Purpose**: Crear las entidades de dominio, enums, interfaces y DTOs que son prerequisito de toda la feature.

- [X] T001 [P] Create `WaitlistOpportunityStatus` enum with PgName attributes in `crud_service/src/CrudService.Domain/Enums/WaitlistOpportunityStatus.cs`
- [X] T002 [P] Create `WaitlistOpportunity` entity in `crud_service/src/CrudService.Domain/Entities/WaitlistOpportunity.cs`
- [X] T003 [P] Create `IWaitlistOpportunityRepository` interface with `FindByWaitlistEntryIdAsync` in `crud_service/src/CrudService.Domain/Interfaces/IWaitlistOpportunityRepository.cs`
- [X] T004 Add `FindActiveByEventAndEmailAsync` method to existing `IWaitlistEntryRepository` in `crud_service/src/CrudService.Domain/Interfaces/IWaitlistEntryRepository.cs`
- [X] T005 [P] Add `WaitlistOpportunityDto` and `WaitlistStatusResponse` DTOs to `crud_service/src/CrudService.Application/Dtos/WaitlistDtos.cs`
- [X] T006 [P] Create `GetWaitlistStatusQuery` record in `crud_service/src/CrudService.Application/UseCases/Waitlist/GetWaitlistStatus/GetWaitlistStatusQuery.cs`
- [X] T007 [P] Create `IGetWaitlistStatusUseCase` interface in `crud_service/src/CrudService.Application/UseCases/Waitlist/GetWaitlistStatus/IGetWaitlistStatusUseCase.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Implementar adaptadores de Infrastructure y registrar DI. Bloquea todas las user stories.

**⚠️ CRITICAL**: No puede comenzarse implementación de user stories hasta completar esta fase.

- [X] T008 Implement `FindActiveByEventAndEmailAsync` in `WaitlistEntryRepository` in `crud_service/src/CrudService.Infrastructure/Persistence/Repositories/WaitlistEntryRepository.cs`
- [X] T009 [P] Create `WaitlistOpportunityRepository` implementing `IWaitlistOpportunityRepository` (returns null if table doesn't exist) in `crud_service/src/CrudService.Infrastructure/Persistence/Repositories/WaitlistOpportunityRepository.cs`
- [X] T010 Add `DbSet<WaitlistOpportunity>` and EF Core configuration to `crud_service/src/CrudService.Infrastructure/Persistence/TicketingDbContext.cs`
- [X] T011 Register `IWaitlistOpportunityRepository`, `WaitlistOpportunityRepository`, `IGetWaitlistStatusUseCase`, and `GetWaitlistStatusHandler` in `crud_service/src/CrudService.Infrastructure/DependencyInjection.cs`

**Checkpoint**: Infrastructure lista — se puede comenzar con las user stories.

---

## Phase 3: User Story 1 — Consulta de inscripción activa sin oportunidad (Priority: P1) 🎯 MVP

**Goal**: Un comprador inscrito consulta su estado y ve inscripción activa sin oportunidad asignada.

**Independent Test**: Crear inscripción activa, consultar con GET, verificar 200 con entry y opportunity null.

### Tests for User Story 1

> **RED primero: escribir estos tests y verificar que FALLAN antes de implementar**

- [X] T012 [US1] Write unit test `ReturnsEntryWithNullOpportunity_WhenActiveEntryExistsWithoutOpportunity` for handler in `crud_service/tests/CrudService.Application.Tests/Waitlist/GetWaitlistStatusHandlerTests.cs` — TC-HU2-01

### Implementation for User Story 1

- [X] T013 [US1] Implement `GetWaitlistStatusHandler` with logic: find active entry by event+email, find opportunity by entry id, project to `WaitlistStatusResponse` — in `crud_service/src/CrudService.Application/UseCases/Waitlist/GetWaitlistStatus/GetWaitlistStatusHandler.cs`
- [X] T014 [US1] Add `GET` action `GetWaitlistStatus` to `WaitlistController` accepting `eventId` and `email` query params, delegating to `IGetWaitlistStatusUseCase`, returning 200 with `WaitlistStatusResponse` — in `crud_service/src/CrudService.Api/Controllers/WaitlistController.cs`

**Checkpoint**: US1 funcional — `GET /api/waitlist/entries?eventId=42&email=...` devuelve 200 con inscripción activa y oportunidad null.

---

## Phase 4: User Story 2 — Consulta con oportunidad activa (Priority: P1)

**Goal**: Un comprador con oportunidad activa consulta su estado y ve la entrada reservada con tiempo restante.

**Independent Test**: Inscripción con oportunidad activa, verificar que respuesta incluye `remainingMinutes` > 0.

### Tests for User Story 2

- [X] T015 [US2] Write unit test `ReturnsOpportunityWithRemainingMinutes_WhenOpportunityIsActive` for handler in `crud_service/tests/CrudService.Application.Tests/Waitlist/GetWaitlistStatusHandlerTests.cs` — TC-HU2-02

### Implementation for User Story 2

- [X] T016 [US2] Add remaining minutes calculation logic `Math.Max(0, (int)(expiresAt - DateTime.UtcNow).TotalMinutes)` to handler projection in `crud_service/src/CrudService.Application/UseCases/Waitlist/GetWaitlistStatus/GetWaitlistStatusHandler.cs`

**Checkpoint**: US2 funcional — oportunidad activa muestra `remainingMinutes` calculado dinámicamente.

---

## Phase 5: User Story 3 — Consulta cuando no existe inscripción (Priority: P2)

**Goal**: Un comprador sin inscripción recibe 404 al consultar.

**Independent Test**: Consultar con email/eventId sin inscripción, verificar 404.

### Tests for User Story 3

- [X] T017 [US3] Write unit test `ReturnsNull_WhenNoEntryFound` for handler in `crud_service/tests/CrudService.Application.Tests/Waitlist/GetWaitlistStatusHandlerTests.cs`

### Implementation for User Story 3

- [X] T018 [US3] Add 404 handling in controller `GET` action: when handler returns null, return `NotFound` with descriptive message — in `crud_service/src/CrudService.Api/Controllers/WaitlistController.cs`
- [X] T019 [US3] Add parameter validation in controller: return 400 if `eventId` or `email` missing/empty, return 400 if email format invalid — in `crud_service/src/CrudService.Api/Controllers/WaitlistController.cs`

**Checkpoint**: US3 funcional — 404 para inscripción inexistente, 400 para parámetros inválidos.

---

## Phase 6: User Story 4 — Consulta con oportunidad consumida o expirada (Priority: P2)

**Goal**: Un comprador ve estado terminal de su oportunidad (consumed o expired).

**Independent Test**: Oportunidad consumed → status "consumed", remainingMinutes 0. Oportunidad expired → status "expired", remainingMinutes 0.

### Tests for User Story 4

- [X] T020 [P] [US4] Write unit test `ReturnsOpportunityConsumed_WhenOpportunityStatusIsConsumed` for handler in `crud_service/tests/CrudService.Application.Tests/Waitlist/GetWaitlistStatusHandlerTests.cs` — TC-HU2-03 (parcial)
- [X] T021 [P] [US4] Write unit test `ReturnsOpportunityExpired_WhenOpportunityStatusIsExpired` for handler in `crud_service/tests/CrudService.Application.Tests/Waitlist/GetWaitlistStatusHandlerTests.cs` — TC-HU2-03
- [X] T022 [US4] Write unit test `DistinguishesAllFourStatesCorrectly` exercising all four visible states in sequence in `crud_service/tests/CrudService.Application.Tests/Waitlist/GetWaitlistStatusHandlerTests.cs` — TC-HU2-04

### Implementation for User Story 4

- [X] T023 [US4] Ensure handler projection sets `remainingMinutes = 0` for non-active opportunity statuses (consumed, expired, failed) in `crud_service/src/CrudService.Application/UseCases/Waitlist/GetWaitlistStatus/GetWaitlistStatusHandler.cs`

**Checkpoint**: US4 funcional — los cuatro estados visibles se proyectan correctamente.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validaciones finales y limpieza.

- [X] T024 [P] Add email normalization (ToLowerInvariant) before query in handler in `crud_service/src/CrudService.Application/UseCases/Waitlist/GetWaitlistStatus/GetWaitlistStatusHandler.cs`
- [X] T025 Run `quickstart.md` validation: execute all test commands and verify `dotnet build` succeeds

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: Sin dependencias — puede comenzar inmediatamente. T001-T003 son paralelos. T004 es independiente. T005-T007 son paralelos.
- **Phase 2 (Foundational)**: Depende de Phase 1. T008 depende de T004. T009 depende de T003. T010 depende de T001, T002. T011 depende de T007, T009, T013.
- **Phase 3 (US1)**: Depende de Phase 2 — BLOQUEANTE para el primer test.
- **Phase 4 (US2)**: Depende de Phase 3 (handler ya implementado).
- **Phase 5 (US3)**: Depende de Phase 3 (controller ya implementado). Puede ejecutarse en paralelo con Phase 4.
- **Phase 6 (US4)**: Depende de Phase 3 (handler ya implementado). Puede ejecutarse en paralelo con Phase 4 y 5.
- **Phase 7 (Polish)**: Depende de todas las fases anteriores.

### User Story Dependencies

- **US1 (P1)**: Depende de Phase 2 — no depende de otras stories.
- **US2 (P1)**: Depende de US1 (handler base debe existir para agregar lógica de cálculo).
- **US3 (P2)**: Depende de US1 (controller debe existir para agregar manejo de 404/400). No depende de US2.
- **US4 (P2)**: Depende de US1 (handler base debe existir). No depende de US2 ni US3.

### Within Each User Story

1. Tests DEBEN escribirse Y FALLAR antes de implementar (RED)
2. Implementación mínima para que pasen (GREEN)
3. Refactor solo si todas las pruebas están en verde

### Parallel Opportunities

```text
Phase 1: T001 ─┐
         T002 ─┤ (paralelos — archivos distintos)
         T003 ─┤
         T005 ─┤
         T006 ─┤
         T007 ─┘
         T004 ─── (independiente)

Phase 2: T008 ───── T009 (paralelos — archivos distintos)
         T010 ───── T011 (secuenciales — mismo archivo o dependencia)

Phase 3-6: US1 primero (secuencial)
           US2, US3, US4 pueden ser paralelos entre sí después de US1
           T020, T021 paralelos (archivos distintos)

Phase 7: T024 ─── T025 (T025 depende de todo)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T007)
2. Complete Phase 2: Foundational (T008-T011)
3. Complete Phase 3: US1 (T012-T014)
4. **STOP and VALIDATE**: Test US1 independientemente — `dotnet test --filter "GetWaitlistStatus"`
5. Deploy/demo si está listo

### Incremental Delivery

1. Setup + Foundational → Infrastructure lista
2. US1 → Test → Deploy (MVP: consulta básica funciona)
3. US2 → Test → Deploy (agrega cálculo de tiempo restante)
4. US3 + US4 → Test → Deploy (errores y estados terminales)
5. Polish → Validar quickstart → Feature completa

### Commit Convention (TDD Traceability)

```text
test(red): add GetWaitlistStatusHandler test for active entry without opportunity
feat(green): implement GetWaitlistStatusHandler returning entry with null opportunity
test(red): add handler test for active opportunity with remaining minutes
feat(green): add remaining minutes calculation to handler
test(red): add handler test for null entry returning null
feat(green): add 404 handling in WaitlistController GET action
test(red): add handler tests for consumed and expired opportunity states
feat(green): ensure handler projects terminal states with zero remaining minutes
refactor: extract email normalization to shared method
```
