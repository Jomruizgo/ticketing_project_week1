# Tasks: Waitlist Opportunity Status & Claim UI

**Input**: Design documents from `/specs/008-waitlist-opportunity-ui/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Incluidos — TDD estricto requerido por constitución y plan de implementación.

**Organization**: Tareas agrupadas por historia de usuario. Backend (ClaimOpportunity use case) es fase fundacional que habilita las historias de claim del frontend.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivos diferentes, sin dependencias)
- **[Story]**: Historia de usuario asociada (US1–US7 según spec.md)
- Rutas exactas incluidas en cada tarea

---

## Phase 1: Setup (Tipos y contratos compartidos)

**Purpose**: Definir tipos de datos en backend y frontend sin lógica de negocio

- [X] T001 [P] Add ClaimOpportunityRequest and ClaimOpportunityResponse DTOs to crud_service/src/CrudService.Application/Dtos/WaitlistDtos.cs
- [X] T002 [P] Add WaitlistOpportunityDto, WaitlistStatusResponse, and ClaimOpportunityResponse types to frontend/lib/types.ts

---

## Phase 2: Foundational — Backend ClaimOpportunity Use Case

**Purpose**: Implementar el caso de uso ClaimOpportunity en el CRUD Service. DEBE completarse antes de las historias de frontend que involucran claim (US2, US5, US6).

**⚠️ CRITICAL**: El endpoint POST /api/waitlist/opportunities/{id}/claim no existe aún. Todo el flujo de claim del frontend depende de este backend.

- [X] T003 [P] Create IClaimOpportunityUseCase interface, ClaimOpportunityCommand record, and ClaimOpportunityResult/ClaimOpportunityResultType types in crud_service/src/CrudService.Application/UseCases/Waitlist/ClaimOpportunity/
- [X] T004 RED — Write unit tests for ClaimOpportunityHandler (happy path Active→Consumed, reject expired opportunity, reject wrong buyer email, idempotent already-consumed) in crud_service/tests/CrudService.Application.Tests/Waitlist/ClaimOpportunityHandlerTests.cs
- [X] T005 GREEN — Implement ClaimOpportunityHandler in crud_service/src/CrudService.Application/UseCases/Waitlist/ClaimOpportunity/ClaimOpportunityHandler.cs
- [X] T006 GREEN — Add POST /api/waitlist/opportunities/{id}/claim endpoint to crud_service/src/CrudService.Api/Controllers/WaitlistController.cs and register IClaimOpportunityUseCase → ClaimOpportunityHandler in crud_service/src/CrudService.Infrastructure/DependencyInjection.cs
- [ ] T007 RED — Integration test for claim persistence (Active→Consumed transition persists correctly) in crud_service/tests/CrudService.Infrastructure.Tests/

**Checkpoint**: Backend ClaimOpportunity completo — endpoint operativo, 4+ tests unitarios GREEN, integración verificada.

---

## Phase 3: User Story 1 — Consulta de estado "En espera" (Priority: P1) 🎯 MVP

**Goal**: El comprador inscrito consulta su estado y ve "En espera" cuando no tiene oportunidad asignada.

**Independent Test**: Inscribir comprador (HU7), navegar a /buy/{eventId}, ingresar email → ver "Inscripción activa" sin botón de pago ni cuenta regresiva.

### Tests for User Story 1

- [X] T008 [P] [US1] RED — Write tests for getWaitlistStatus(eventId, email) API function (200 with entry only, 200 with entry+opportunity) in frontend/tests/lib/api-waitlist-status.test.ts
- [X] T009 [P] [US1] RED — Write tests for WaitlistStatus component "en espera" state (renders badge "En espera", no countdown, no claim button) in frontend/tests/components/waitlist-status.test.tsx

### Implementation for User Story 1

- [X] T010 [US1] GREEN — Implement getWaitlistStatus(eventId, email) calling GET /api/waitlist/entries?eventId={id}&email={email} in frontend/lib/api.ts
- [X] T011 [US1] GREEN — Create WaitlistStatus component with email query form and "En espera" conditional rendering in frontend/components/waitlist-status.tsx
- [X] T012 [US1] GREEN — Integrate WaitlistStatus in frontend/app/buy/[id]/page.tsx replacing or complementing WaitlistEnrollForm for already-enrolled buyers

**Checkpoint**: US1 funcional — comprador consulta estado y ve "Inscripción activa – En espera" sin acciones de pago.

---

## Phase 4: User Story 2 — Oportunidad activa con cuenta regresiva y claim (Priority: P1) 🎯 MVP

**Goal**: El comprador con oportunidad activa ve cuenta regresiva MM:SS, hace clic en "Avanzar al pago", consume la oportunidad y es redirigido al flujo de pago con ticketId.

**Independent Test**: Crear oportunidad activa, consultar estado → cuenta regresiva visible + botón "Avanzar al pago" → click → redirección a PaymentForm con ticketId.

### Tests for User Story 2

- [X] T013 [P] [US2] RED — Write tests for claimOpportunity(opportunityId, buyerEmail) API function (200 returns ClaimOpportunityResponse, throws ApiError for non-200) in frontend/tests/lib/api-waitlist-status.test.ts
- [X] T014 [P] [US2] RED — Write tests for useWaitlistSse hook (connects to SSE stream at /api/waitlist/stream?email={email}, handles opportunity_activated event, calls onEvent callback) in frontend/tests/hooks/use-waitlist-sse.test.ts

### Implementation for User Story 2

- [X] T015 [P] [US2] GREEN — Implement claimOpportunity(opportunityId, buyerEmail) calling POST /api/waitlist/opportunities/{id}/claim in frontend/lib/api.ts
- [X] T016 [P] [US2] GREEN — Create useWaitlistSse(email, onEvent) hook using EventSource API with reconnection and GET sync on reconnect in frontend/hooks/use-waitlist-sse.ts
- [X] T017 [US2] RED — Write tests for countdown timer (MM:SS format, decrements every second, disables button at 00:00), claim button click → loading state → redirect to payment, and SSE opportunity_activated → active state transition in frontend/tests/components/waitlist-status.test.tsx
- [X] T018 [US2] GREEN — Add countdown timer (setInterval 1000ms, format MM:SS from expiresAt), claim button with loading/disabled states, redirect to payment flow with ticketId, and SSE opportunity_activated integration to frontend/components/waitlist-status.tsx

**Checkpoint**: US2 funcional — cuenta regresiva MM:SS, botón "Avanzar al pago" → claim → redirect con ticketId, SSE opportunity_activated transiciona UI en tiempo real.

---

## Phase 5: User Story 3 — Oportunidad expirada (Priority: P2)

**Goal**: El comprador cuya oportunidad venció ve "Oportunidad expirada" sin acciones disponibles.

**Independent Test**: Crear oportunidad, esperar expiración → UI muestra "Oportunidad expirada" sin botón ni cuenta regresiva.

- [X] T019 [US3] RED — Write tests for expired state rendering (badge "Oportunidad expirada", no claim button, no countdown) and SSE opportunity_expired event → transitions UI to expired state in frontend/tests/components/waitlist-status.test.tsx
- [X] T020 [US3] GREEN — Add expired state rendering branch and opportunity_expired SSE event handling in frontend/components/waitlist-status.tsx and frontend/hooks/use-waitlist-sse.ts

**Checkpoint**: US3 funcional — oportunidad expirada muestra badge sin acciones, SSE opportunity_expired actualiza UI en tiempo real.

---

## Phase 6: User Story 4 — Oportunidad consumida (Priority: P2)

**Goal**: El comprador que ya reclamó su oportunidad ve "Oportunidad utilizada" al revisitar la página.

**Independent Test**: Consumir oportunidad, regresar a /buy/{eventId} → ver "Oportunidad utilizada" sin botón de pago.

- [X] T021 [US4] RED — Write tests for consumed state rendering (badge "Oportunidad utilizada", no claim button, no countdown) in frontend/tests/components/waitlist-status.test.tsx
- [X] T022 [US4] GREEN — Add consumed state rendering branch to frontend/components/waitlist-status.tsx

**Checkpoint**: US4 funcional — oportunidad consumida muestra badge de confirmación sin acciones de pago.

---

## Phase 7: User Stories 5, 6, 7 — Error Handling (Priority: P2/P3)

**Goal**: Manejo robusto de errores de claim (409, 403) y consulta (404) con mensajes informativos.

### User Story 5 — Claim 409: oportunidad ya no activa (P2)

- [X] T023 [US5] RED — Write test for claim 409 response: shows toast "La oportunidad ya no está activa" and transitions view to expired state in frontend/tests/components/waitlist-status.test.tsx
- [X] T024 [US5] GREEN — Handle 409 ApiError in claim flow with toast and expired state transition in frontend/components/waitlist-status.tsx

### User Story 6 — Claim 403: email incorrecto (P3)

- [X] T025 [US6] RED — Write test for claim 403 response: shows toast "Esta oportunidad no pertenece al comprador indicado" in frontend/tests/components/waitlist-status.test.tsx
- [X] T026 [US6] GREEN — Handle 403 ApiError in claim flow with toast notification in frontend/components/waitlist-status.tsx

### User Story 7 — 404: no encontrado (P3)

- [X] T027 [US7] RED — Write tests for query 404 "No existe inscripción para este comprador" display and claim 404 "Oportunidad no encontrada" error in frontend/tests/components/waitlist-status.test.tsx
- [X] T028 [US7] GREEN — Handle 404 in getWaitlistStatus (show "no inscripción" message) and 404 in claimOpportunity (show "no encontrada" error) in frontend/components/waitlist-status.tsx

**Checkpoint**: Todos los errores documentados muestran mensajes informativos. FR-008, FR-009, FR-010, FR-014, FR-016 cubiertos.

### Cross-cutting — Errores de red y respuestas inesperadas (FR-016)

- [X] T029 [US5] RED — Write tests for network failure (TypeError: Failed to fetch) and 5xx responses showing generic error message with retry button in frontend/tests/components/waitlist-status.test.tsx
- [X] T030 [US5] GREEN — Handle network errors and 5xx responses in both getWaitlistStatus and claimOpportunity flows with generic error toast and retry action in frontend/components/waitlist-status.tsx

**Checkpoint**: FR-016 completamente cubierto — errores de red y 5xx muestran mensaje genérico con opción de reintento.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Validación de aceptación y limpieza final

- [X] T031 Run quickstart.md acceptance validation (TC-HU8-01 through TC-HU8-04) and verify all unit tests pass (backend: dotnet test, frontend: npm test)
- [X] T032 Refactor pass: cleanup duplicated code, verify naming consistency, remove dead code across all modified files

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: T003 depends on T001 (backend DTOs needed for handler types). T002 can run in parallel with Phase 2
- **US1 (Phase 3)**: Depends on T002 (frontend types). Does NOT depend on Phase 2 (no claim needed for status query)
- **US2 (Phase 4)**: Depends on Phase 3 (WaitlistStatus component must exist). Phase 2 must be complete for production claim
- **US3 (Phase 5)**: Depends on T016/T018 (SSE hook and component active state must exist)
- **US4 (Phase 6)**: Depends on Phase 3 (component must exist). Can run in parallel with Phase 4/5
- **US5+US6 (Phase 7)**: Depends on T018 (claim flow must be implemented in component)
- **US7 (Phase 7)**: Depends on T011 (query flow must be implemented in component)
- **Polish (Phase 8)**: Depends on all preceding phases

### User Story Dependencies

```
Phase 1 ──┬── Phase 2 (Backend) ──────────────────────────────┐
           │                                                    │
           └── Phase 3 (US1: Query) ──┬── Phase 4 (US2: Claim) ┼── Phase 7 (US5/US6/US7)
                                      │         │               │
                                      │         └── Phase 5 (US3)│
                                      │                         │
                                      └── Phase 6 (US4) ───────┘── Phase 8 (Polish)
```

### Within Each User Story (TDD)

1. Write failing tests (RED) → verify they fail
2. Implement minimum code to pass (GREEN) → verify tests pass
3. Refactor if needed → verify tests still pass
4. Commit with convention: `test(red): ...` → `feat(green): ...` → `refactor: ...`

### Parallel Opportunities

- **Phase 1**: T001 ‖ T002 (backend DTOs ‖ frontend types — different codebases)
- **Phase 2**: T002 can run in parallel with T003–T007 (frontend types don't depend on backend logic)
- **Phase 3**: T008 ‖ T009 (API tests ‖ component tests — different test files)
- **Phase 4**: T013 ‖ T014 (API tests ‖ SSE hook tests — different files); T015 ‖ T016 (API impl ‖ hook impl — different files)
- **Phase 6**: Can run in parallel with Phase 4/5 (consumed state doesn't depend on claim/SSE implementation)

---

## Parallel Example: Phase 3 (US1)

```bash
# Launch parallel RED tests:
Task T008: "Tests for getWaitlistStatus() in frontend/tests/lib/api-waitlist-status.test.ts"
Task T009: "Tests for WaitlistStatus 'en espera' in frontend/tests/components/waitlist-status.test.tsx"

# Then sequential GREEN:
Task T010: "Implement getWaitlistStatus() in frontend/lib/api.ts"
Task T011: "Create WaitlistStatus component in frontend/components/waitlist-status.tsx"
Task T012: "Integrate in frontend/app/buy/[id]/page.tsx"
```

---

## Implementation Strategy

### MVP First (US1 + US2 Only)

1. Complete Phase 1: Setup (types/DTOs)
2. Complete Phase 2: Backend ClaimOpportunity (endpoint operativo)
3. Complete Phase 3: US1 — Status query "En espera"
4. Complete Phase 4: US2 — Countdown + Claim + SSE
5. **STOP and VALIDATE**: Test TC-HU8-01, TC-HU8-02, TC-HU8-03 independently
6. Deploy/demo if ready — covers core conversion flow

### Incremental Delivery

1. Setup + Foundational → Backend ready
2. US1 → Status query functional → Partial demo
3. US2 → Full claim flow functional → **MVP complete** (TC-HU8-01, TC-HU8-02, TC-HU8-03)
4. US3 + US4 → State lifecycle complete → TC-HU8-04
5. US5 + US6 + US7 → Error handling robust → Production-ready
6. Polish → Validated, clean, refactored

---

## Notes

- Backend usa `ClaimOpportunityResult` con enum de variantes (patrón existente en AssignOpportunityResult, ExpireOpportunityResult)
- Frontend reutiliza `ApiError` de api.ts para mapear códigos HTTP a mensajes de error
- La cuenta regresiva calcula `Math.max(0, differenceInSeconds(expiresAt, now))` localmente con setInterval(1000)
- El SSE hook sigue la API EventSource con reconexión automática + GET sync al reconectar (FR-017)
- El botón "Avanzar al pago" se deshabilita durante el claim (FR-015) y cuando countdown llega a 00:00 (FR-005)
- T007 (integración con Testcontainers) puede requerir agregar el paquete NuGet `Testcontainers.PostgreSql` al proyecto CrudService.Infrastructure.Tests si no está instalado
