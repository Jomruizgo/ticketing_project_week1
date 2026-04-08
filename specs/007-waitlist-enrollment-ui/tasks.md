# Tasks: Waitlist Enrollment UI

**Input**: Design documents from `/specs/007-waitlist-enrollment-ui/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Se implementan tests unitarios con vitest + @testing-library/react siguiendo TDD (RED→GREEN→REFACTOR). Los test cases TC-HU7-01 a TC-HU7-04 (tipo A, aceptación) se validan manualmente como validación final (T010).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Establecer la infraestructura de testing para el frontend

- [x] T001 Install vitest, @testing-library/react, @testing-library/jest-dom, @testing-library/user-event, and jsdom as devDependencies; create vitest.config.ts and frontend/tests/setup.ts in frontend/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Tipo y función API que DEBEN existir antes de cualquier implementación de user story

**⚠️ CRITICAL**: No se puede crear el componente WaitlistEnrollForm ni modificar la página de compra sin estos artefactos

- [x] T002 [P] Add WaitlistEntryDto interface to frontend/lib/types.ts
- [x] T003 [P] Add enrollInWaitlist() function to the api object in frontend/lib/api.ts

**Checkpoint**: Foundation ready — el tipo WaitlistEntryDto y la función enrollInWaitlist() están disponibles para el componente de UI

---

## Phase 3: User Story 1 — Inscripción exitosa en lista de espera (Priority: P1) 🎯 MVP

**Goal**: El comprador puede inscribirse en la lista de espera de un evento sin disponibilidad y con fecha futura. La confirmación se muestra con estado activo.

**Independent Test**: Crear un evento sin entradas disponibles con fecha futura, navegar a /buy/{eventId}, completar el formulario de inscripción con email válido, verificar confirmación con estado activo.

**Covers**: FR-001, FR-003, FR-004, FR-005, FR-006, FR-007, FR-008, FR-010, FR-011, FR-012

### Tests for User Story 1 (RED phase — write FIRST, must FAIL)

- [x] T004 [US1] test(red): Create unit tests for WaitlistEnrollForm component in frontend/tests/components/waitlist-enroll-form.test.tsx — test cases: renders email input and submit button, validates empty email, validates invalid email format, calls enrollInWaitlist on valid submit, shows success confirmation on 201, shows informative message on 409-duplicate, shows closed message on 422, shows generic error on network failure, disables button during loading
- [x] T005 [P] [US1] test(red): Create unit tests for enrollInWaitlist API function in frontend/tests/lib/api.test.ts — test cases: sends POST with correct payload, returns WaitlistEntryDto on 201, throws ApiError with status 409 on duplicate, throws ApiError with status 422 on closed, throws ApiError with status 404 on not found, throws ApiError on network error

### Implementation for User Story 1 (GREEN phase)

- [x] T006 [US1] feat(green): Create WaitlistEnrollForm component in frontend/components/waitlist-enroll-form.tsx
- [x] T007 [US1] feat(green): Add conditional rendering logic in frontend/app/buy/[id]/page.tsx — show WaitlistEnrollForm when availableTickets === 0 and event date is in the future, show "Lista de espera cerrada" when availableTickets === 0 and event date has passed, preserve existing purchase flow when availableTickets > 0. Covers: FR-001, FR-002, FR-009
- [x] T008 [US1] feat(green): Modify buyer-event-card to show waitlist CTA when availableTickets === 0 and event is upcoming in frontend/components/buyer-event-card.tsx. Covers: FR-012

**Checkpoint**: US1 complete — el comprador puede inscribirse exitosamente en la lista de espera desde la página de compra. Todos los tests de T004 y T005 deben estar en verde.

---

## Phase 4: User Story 4 — Flujo de compra normal preservado (Priority: P1)

**Goal**: Cuando hay entradas disponibles, el flujo de compra existente se muestra sin cambios. El formulario de waitlist no es visible.

**Independent Test**: Navegar a /buy/{eventId} de un evento con entradas disponibles, verificar que se muestra el flujo de compra normal y que el formulario de waitlist no aparece.

**Covers**: FR-002

### Implementation for User Story 4

_No se requieren tareas adicionales de implementación._ La lógica condicional de T007 ya garantiza que el flujo de compra existente se preserve cuando `availableTickets > 0`.

**Checkpoint**: US4 complete — el flujo de compra normal funciona sin alteraciones

---

## Phase 5: User Story 2 — Inscripción duplicada rechazada (Priority: P2)

**Goal**: Si el comprador ya está inscrito (respuesta 409), la aplicación muestra un mensaje informativo sin crear duplicado.

**Independent Test**: Inscribir un email, intentar inscribir el mismo email en el mismo evento, verificar mensaje informativo de duplicado.

**Covers**: FR-006

### Implementation for User Story 2

_No se requieren tareas adicionales de implementación._ El manejo de la respuesta 409 ya está incluido en T006 (WaitlistEnrollForm) y testeado en T004. El componente distingue por código HTTP y muestra el mensaje "Ya tienes una inscripción activa" cuando recibe 409.

**Checkpoint**: US2 complete — el duplicado se rechaza con mensaje informativo

---

## Phase 6: User Story 3 — Lista de espera cerrada por evento pasado (Priority: P3)

**Goal**: Si la fecha del evento ya pasó y no hay entradas, se muestra un mensaje de lista cerrada sin formulario.

**Independent Test**: Navegar a /buy/{eventId} de un evento con fecha pasada y sin entradas, verificar que no se muestra el formulario y sí el mensaje de lista cerrada.

**Covers**: FR-009, FR-007

### Implementation for User Story 3

_No se requieren tareas adicionales de implementación._ FR-009 (validación preventiva client-side): la lógica condicional de T007 incluye la rama `startsAt <= now` que renderiza el mensaje de lista cerrada. FR-007 (fallback server-side 422): el manejo en T006 muestra el mensaje de lista cerrada cuando recibe 422 del servidor.

**Checkpoint**: US3 complete — el mensaje de lista cerrada se muestra para eventos pasados

---

## Phase 7: User Story 5 — Error de red con reintento (Priority: P2)

**Goal**: Si la solicitud falla por error de red o error del servidor, se muestra un error genérico y se permite reintentar.

**Independent Test**: Simular fallo de red o error 500, verificar mensaje de error amigable y que el formulario permite reintento.

**Covers**: FR-010

### Implementation for User Story 5

_No se requieren tareas adicionales de implementación._ T003 (enrollInWaitlist) lanza ApiError para cualquier respuesta no exitosa y para fallos de red (catch en fetch). T006 (WaitlistEnrollForm) captura estos errores y muestra un mensaje genérico con la posibilidad de reintentar (el formulario vuelve a estado idle). Testeado en T004 y T005.

**Checkpoint**: US5 complete — los errores de red se manejan con mensaje y reintento

---

## Phase 8: Refactor & Acceptance Validation

**Purpose**: Refactorización post-GREEN y validación final de aceptación

- [x] T009 refactor: Review and refactor WaitlistEnrollForm, enrollInWaitlist, and conditional rendering — extract shared logic if duplicated, ensure naming consistency, remove dead code (only if all tests pass)
- [x] T010 Run quickstart.md validation (TC-HU7-01 through TC-HU7-04) against deployed system

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — T001 installs test infra
- **Foundational (Phase 2)**: Depends on Phase 1 (types need vitest config for RED tests)
- **User Stories (Phases 3-7)**: All depend on Phase 2 completion
  - US1 (Phase 3) has all implementation tasks (T004-T008)
  - US2, US3, US4, US5 (Phases 4-7) are covered by T006/T007 behavior — no additional code needed
- **Refactor & Validation (Phase 8)**: T009 requires all tests green. T010 depends on all phases complete.

### User Story Dependencies

- **US1 (P1)**: Depends on T001 (testing), T002, T003 (foundational types and API)
- **US4 (P1)**: No additional tasks — behavior inherent in T007 conditional logic
- **US2 (P2)**: No additional tasks — behavior inherent in T006 error handling (tested in T004)
- **US3 (P3)**: No additional tasks — FR-009 in T007, FR-007 in T006 (tested in T004)
- **US5 (P2)**: No additional tasks — behavior inherent in T003/T006 error handling (tested in T004, T005)

### Task-Level Dependencies

```
T001 (setup vitest)
  │
  ├──► T002 ──┐
  │           ├──► T004 (test RED) ──► T006 (impl GREEN)
  └──► T003 ──┘                              │
                    T005 (test RED) ──► T003 ◄┘
                                         │
                                    T007 (conditional rendering)
                                         │
                    T008 (buyer-event-card, parallelizable with T006/T007)
                                         │
                                    T009 (refactor)
                                         │
                                    T010 (acceptance validation)
```

### Within Each User Story (TDD cycle)

1. Tests (T004, T005) MUST be written and FAIL before implementation (RED)
2. Implementation (T006, T007, T008) makes tests pass (GREEN)
3. Refactor (T009) only if all tests are green (REFACTOR)
4. Commits follow: `test(red): ...` → `feat(green): ...` → `refactor: ...`

### Parallel Opportunities

- T002 and T003 can run in parallel (different files: types.ts vs api.ts)
- T004 and T005 can run in parallel (different test files)
- T008 can run in parallel with T006/T007 (buyer-event-card.tsx is independent)

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001 — install vitest)
2. Complete Phase 2: Foundational (T002 + T003 in parallel)
3. RED: Write tests (T004 + T005 in parallel) — verify they FAIL
4. GREEN: Implement (T006 → T007 → T008) — verify tests PASS
5. **STOP and VALIDATE**: Run test suite + manual TC-HU7-01 through TC-HU7-04
6. Deploy/demo if ready — inscripción funcional end-to-end

### Single Developer Strategy

1. T001 → T002 → T003 → T004 → T005 → T006 → T007 → T008 → T009 → T010
2. Commits follow TDD convention: `test(red):` → `feat(green):` → `refactor:`
3. TC-HU7-* manual validation after T008 (core feature complete)

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US2, US3, US4, US5 have NO implementation tasks — their behaviors are inherent in the T006/T007 implementation and tested in T004/T005
- TC-HU7-01 validates US1 + US4 simultaneously
- TC-HU7-02 validates US1 (happy path)
- TC-HU7-03 validates US3
- TC-HU7-04 validates US2
- TDD convention: `test(red): ...` → `feat(green): ...` → `refactor: ...`
- FR-007 (server-side 422 fallback) is distinct from FR-009 (client-side date check): both are tested in T004 but cover different scenarios
