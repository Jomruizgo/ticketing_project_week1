# Tasks: Inscripción en Lista de Espera

**Input**: Design documents from `/specs/001-waitlist-enrollment/`  
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/waitlist-api.md, quickstart.md

**Tests**: Incluidos — el plan y los TestCases.md exigen TDD estricto (TC-HU1-01 a TC-HU1-05).

**Organization**: Tareas agrupadas por user story para habilitar implementación y testing independiente de cada historia.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivos distintos, sin dependencias de tareas incompletas)
- **[Story]**: A qué user story pertenece (US1, US2, US3)
- Rutas exactas de archivo incluidas en cada tarea

---

## Phase 1: Setup (Infraestructura compartida)

**Purpose**: Esquema de BD, enum PostgreSQL y configuración base del proyecto de tests

- [X] T001 Agregar tipo enum `waitlist_entry_status` ('active', 'consumed', 'expired') y tabla `waitlist_entries` con partial unique index en `scripts/schema.sql`
- [X] T002 [P] Agregar paquete `Testcontainers.PostgreSql` al proyecto `crud_service/tests/CrudService.Infrastructure.Tests/CrudService.Infrastructure.Tests.csproj`
- [X] T003 [P] Crear enum `WaitlistEntryStatus` con atributos `[PgName]` en `crud_service/src/CrudService.Domain/Enums/WaitlistEntryStatus.cs`
- [X] T004 Registrar mapeo del enum `WaitlistEntryStatus` en `NpgsqlConnection.GlobalTypeMapper` en `crud_service/src/CrudService.Api/Program.cs`

---

## Phase 2: Foundational (Prerrequisitos bloqueantes)

**Purpose**: Entidad de dominio, puerto (interfaz de repositorio) y excepciones que DEBEN existir antes de cualquier user story

**⚠️ CRITICAL**: Ninguna tarea de user story puede comenzar sin completar esta fase

- [X] T005 Crear entidad `WaitlistEntry` (Id, EventId, BuyerEmail, Status, EnrolledAt, navegación a Event) en `crud_service/src/CrudService.Domain/Entities/WaitlistEntry.cs`
- [X] T006 [P] Crear interfaz `IWaitlistEntryRepository` con métodos `ExistsActiveAsync(long eventId, string buyerEmail)` y `AddAsync(WaitlistEntry entry)` en `crud_service/src/CrudService.Domain/Interfaces/IWaitlistEntryRepository.cs`
- [X] T007 [P] Crear excepción `DuplicateWaitlistEntryException` en `crud_service/src/CrudService.Domain/Exceptions/DuplicateWaitlistEntryException.cs`
- [X] T008 [P] Crear excepción `WaitlistClosedException` en `crud_service/src/CrudService.Domain/Exceptions/WaitlistClosedException.cs`
- [X] T009 Crear DTOs `EnrollInWaitlistRequest` y `WaitlistEntryDto` en `crud_service/src/CrudService.Application/Dtos/WaitlistDtos.cs`
- [X] T010 Crear interfaz `IEnrollInWaitlistUseCase` en `crud_service/src/CrudService.Application/UseCases/Waitlist/EnrollInWaitlist/IEnrollInWaitlistUseCase.cs`
- [X] T011 Crear record `EnrollInWaitlistCommand` en `crud_service/src/CrudService.Application/UseCases/Waitlist/EnrollInWaitlist/EnrollInWaitlistCommand.cs`

**Checkpoint**: Foundation lista — entidad, puerto, excepciones y command definidos. Puede comenzar la implementación de user stories.

---

## Phase 3: User Story 1 — Inscripción exitosa en lista de espera (Priority: P1) 🎯 MVP

**Goal**: Un comprador puede inscribirse exitosamente en la lista de espera de un evento y recibir confirmación con estado "active".

**Independent Test**: Enviar solicitud con evento válido y correo nuevo → 201 con inscripción "active".

### Tests para User Story 1 (TDD RED)

> **Escribir estos tests PRIMERO. Deben FALLAR antes de implementar.**

- [X] T012 [US1] Escribir test `EnrollInWaitlist_ValidRequest_ReturnsActiveEntry` (TC-HU1-01) en `crud_service/tests/CrudService.Application.Tests/Waitlist/EnrollInWaitlistHandlerTests.cs`
- [X] T013 [US1] Escribir test `EnrollInWaitlist_PreviousConsumedEntry_AllowsReenrollment` (TC-HU1-04, variante consumed) en `crud_service/tests/CrudService.Application.Tests/Waitlist/EnrollInWaitlistHandlerTests.cs`
- [X] T014 [US1] Escribir test `EnrollInWaitlist_PreviousExpiredEntry_AllowsReenrollment` (TC-HU1-04, variante expired) en `crud_service/tests/CrudService.Application.Tests/Waitlist/EnrollInWaitlistHandlerTests.cs`

### Implementation para User Story 1 (TDD GREEN)

- [X] T015 [US1] Implementar `EnrollInWaitlistHandler` con validación de evento existente + fecha abierta + creación de inscripción en `crud_service/src/CrudService.Application/UseCases/Waitlist/EnrollInWaitlist/EnrollInWaitlistHandler.cs`
- [X] T016 [US1] Implementar `WaitlistEntryRepository` con `AddAsync` y `ExistsActiveAsync` en `crud_service/src/CrudService.Infrastructure/Persistence/Repositories/WaitlistEntryRepository.cs`
- [X] T017 [US1] Agregar `DbSet<WaitlistEntry>` y configuración del modelo (enum, FK, partial unique index con `HasFilter`) en `crud_service/src/CrudService.Infrastructure/Persistence/TicketingDbContext.cs`
- [X] T018 [US1] Registrar `IWaitlistEntryRepository`, `IEnrollInWaitlistUseCase` y `EnrollInWaitlistHandler` en `crud_service/src/CrudService.Infrastructure/DependencyInjection.cs`
- [X] T019 [US1] Crear `WaitlistController` con `POST /api/waitlist/entries` que delega al use case y mapea excepciones a HTTP 201/404/409/422 en `crud_service/src/CrudService.Api/Controllers/WaitlistController.cs`

**Checkpoint**: US1 completa — inscripción funcional end-to-end, reinscripción tras consumed/expired verificada.

---

## Phase 4: User Story 2 — Rechazo de inscripción duplicada (Priority: P2)

**Goal**: El sistema rechaza con 409 cualquier intento de inscripción cuando ya existe una inscripción activa del mismo comprador para el mismo evento.

**Independent Test**: Crear inscripción activa, enviar segunda solicitud idéntica → 409.

### Tests para User Story 2 (TDD RED)

- [X] T020 [US2] Escribir test `EnrollInWaitlist_ActiveEntryExists_ThrowsDuplicateException` (TC-HU1-02) en `crud_service/tests/CrudService.Application.Tests/Waitlist/EnrollInWaitlistHandlerTests.cs`
- [X] T021 [US2] Escribir test `EnrollInWaitlist_ActiveEntryDifferentEvent_Succeeds` (edge case: unicidad por par evento-comprador) en `crud_service/tests/CrudService.Application.Tests/Waitlist/EnrollInWaitlistHandlerTests.cs`

### Implementation para User Story 2 (TDD GREEN)

- [X] T022 [US2] Agregar validación de duplicado activo en `EnrollInWaitlistHandler` — verificar `ExistsActiveAsync` antes de `AddAsync`, lanzar `DuplicateWaitlistEntryException` en `crud_service/src/CrudService.Application/UseCases/Waitlist/EnrollInWaitlist/EnrollInWaitlistHandler.cs`

**Checkpoint**: US2 completa — duplicados rechazados a nivel de aplicación.

---

## Phase 5: User Story 3 — Rechazo por lista de espera cerrada (Priority: P3)

**Goal**: El sistema rechaza con 422 cualquier inscripción cuando la fecha del evento ya fue alcanzada.

**Independent Test**: Enviar solicitud con evento con fecha pasada → 422.

### Tests para User Story 3 (TDD RED)

- [X] T023 [US3] Escribir test `EnrollInWaitlist_EventDateReached_ThrowsWaitlistClosedException` (TC-HU1-03) en `crud_service/tests/CrudService.Application.Tests/Waitlist/EnrollInWaitlistHandlerTests.cs`
- [X] T024 [US3] Escribir test `EnrollInWaitlist_EventNotFound_ThrowsEventNotFoundException` (edge case: evento inexistente → 404) en `crud_service/tests/CrudService.Application.Tests/Waitlist/EnrollInWaitlistHandlerTests.cs`

### Implementation para User Story 3 (TDD GREEN)

- [X] T025 [US3] Agregar validación de lista cerrada en `EnrollInWaitlistHandler` — verificar `Event.StartsAt <= DateTime.UtcNow`, lanzar `WaitlistClosedException` en `crud_service/src/CrudService.Application/UseCases/Waitlist/EnrollInWaitlist/EnrollInWaitlistHandler.cs`

**Checkpoint**: US3 completa — lista cerrada rechaza todas las inscripciones.

---

## Phase 6: Integración — Unicidad a nivel de base de datos (TC-HU1-05)

**Purpose**: Prueba de integración con Testcontainers para validar que el partial unique index rechaza duplicados incluso sin pasar por la capa de aplicación.

### Tests de integración (TDD RED)

- [ ] T026 Escribir test `AddAsync_DuplicateActiveEntry_ThrowsDuplicateException` (TC-HU1-05) con Testcontainers + IAsyncLifetime en `crud_service/tests/CrudService.Infrastructure.Tests/Integration/WaitlistEntryRepositoryTests.cs`
- [ ] T027 [P] Escribir test `AddAsync_DuplicateAfterConsumed_Succeeds` (complemento: reinscripción a nivel BD) en `crud_service/tests/CrudService.Infrastructure.Tests/Integration/WaitlistEntryRepositoryTests.cs`

### Implementation (TDD GREEN)

- [ ] T028 Agregar manejo de `PostgresException` código `23505` en `WaitlistEntryRepository.AddAsync` para lanzar `DuplicateWaitlistEntryException` en `crud_service/src/CrudService.Infrastructure/Persistence/Repositories/WaitlistEntryRepository.cs`

**Checkpoint**: Unicidad garantizada tanto en aplicación como en BD. Todos los TC-HU1-01 a TC-HU1-05 cubiertos.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validación E2E con quickstart.md y limpieza final

- [ ] T029 [P] Verificar compilación de toda la solución con `dotnet build crud_service/CrudService.sln`
- [ ] T030 [P] Ejecutar todas las pruebas con `dotnet test crud_service/CrudService.sln` y confirmar que pasan
- [ ] T031 Ejecutar validación manual de quickstart.md: levantar Docker Compose, probar los curls del endpoint

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sin dependencias — puede empezar inmediatamente
- **Foundational (Phase 2)**: Depende de Phase 1 (T003 necesita existir antes que T005) — BLOQUEA todas las user stories
- **User Story 1 (Phase 3)**: Depende de Phase 2 — el handler necesita la entidad, el puerto y el command
- **User Story 2 (Phase 4)**: Depende de Phase 3 (T015, el handler base, debe existir para agregar validación de duplicados)
- **User Story 3 (Phase 5)**: Depende de Phase 3 (T015, el handler base, debe existir para agregar validación de fecha)
- **Integración (Phase 6)**: Depende de Phase 3 (repositorio debe estar implementado)
- **Polish (Phase 7)**: Depende de todas las fases anteriores

### User Story Dependencies

- **US1 (P1)**: Depende de Phase 2 — sin dependencias de otras stories
- **US2 (P2)**: Depende de US1 (el handler base T015 debe existir)
- **US3 (P3)**: Depende de US1 (el handler base T015 debe existir)
- **US2 y US3 pueden ejecutarse en paralelo** después de completar US1

### Within Each User Story

- Tests (RED) DEBEN escribirse y FALLAR antes de implementación (GREEN)
- Commits siguen: `test(red):` → `feat(green):` → `refactor:`
- Foundation models antes de services
- Services antes de endpoints
- Core implementation antes de integración

### Parallel Opportunities

- T002, T003: paquete NuGet y enum son archivos independientes → paralelo
- T006, T007, T008: interfaz y excepciones son archivos independientes → paralelo
- T013, T014: variantes del mismo test case (consumed/expired) → paralelo
- T020, T021: tests de US2 son independientes → paralelo
- T023, T024: tests de US3 son independientes → paralelo
- T026, T027: tests de integración son independientes → paralelo
- T029, T030: build y test son independientes → paralelo
- **US2 (Phase 4) y US3 (Phase 5)**: pueden ejecutarse en paralelo tras completar US1

---

## Parallel Example: User Story 1

```text
T012 ─┐
T013 ─┤ (tests RED en paralelo, archivos distintos NO — mismo archivo)
T014 ─┘
      │
      ▼ (todos en rojo, commit: test(red))
      │
T015 ─── Handler implementation
      │
T016 ─┐
T017 ─┤ (repositorio, DbContext, DI — secuencial por dependencias)
T018 ─┘
      │
T019 ─── Controller
      │
      ▼ (todo en verde, commit: feat(green))
```

## Implementation Strategy

1. **MVP**: Phase 1 + Phase 2 + Phase 3 (US1) = endpoint funcional con inscripción exitosa y reinscripción
2. **Incremento 1**: Phase 4 (US2) = rechazo de duplicados a nivel de aplicación
3. **Incremento 2**: Phase 5 (US3) = rechazo por lista cerrada
4. **Incremento 3**: Phase 6 = garantía de unicidad a nivel de BD (integración)
5. **Cierre**: Phase 7 = validación completa y polish
