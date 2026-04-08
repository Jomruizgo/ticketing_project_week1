# Tasks: Expiración de Oportunidad de Lista de Espera

**Input**: Design documents from `/specs/006-opportunity-expiration/`
**Prerequisites**: plan.md (loaded), spec.md (loaded), research.md (loaded), data-model.md (loaded), quickstart.md (loaded)

**Tests**: Sí — el hu6-plan.md exige TDD y la spec define 5 test cases mínimos (TC-HU6-01 a TC-HU6-05).

**Organization**: Tasks agrupadas por user story para posibilitar implementación y testing independientes. Cada user story es un incremento entregable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivos distintos, sin dependencias sobre tareas incompletas)
- **[Story]**: US1, US2, US3 (mapean a user stories de spec.md)
- Rutas exactas de archivos incluidas en cada tarea

## Path Conventions

```text
crud_service/src/CrudService.Domain/          # Puertos e interfaces
crud_service/src/CrudService.Application/     # Use cases y handlers
crud_service/src/CrudService.Infrastructure/  # Adaptadores (RabbitMQ, EF Core, repos)
crud_service/tests/CrudService.Application.Tests/  # Tests unitarios
crud_service/tests/CrudService.Infrastructure.Tests/  # Tests de integración
scripts/schema.sql                             # Esquema de BD
```

---

## Phase 1: Setup

**Purpose**: Preparar la estructura de directorios y artefactos del use case

- [X] T001 Crear directorio `crud_service/src/CrudService.Application/UseCases/Waitlist/ExpireOpportunity/`
- [X] T002 [P] Agregar columnas `expired_at TIMESTAMPTZ NULL` y `expiration_reason VARCHAR(100) NULL` a la tabla `waitlist_opportunities` en `scripts/schema.sql`
- [X] T002b [P] Verificar que `WAITLIST_OPPORTUNITY_TTL_MS` está declarada en `compose.yml` para el servicio crud-service (o documentar explícitamente que el TTL es exclusivo de la topología DLX en `scripts/setup-rabbitmq.sh`)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Puertos y extensiones de dominio/infraestructura que DEBEN completarse antes de cualquier user story

**⚠️ CRITICAL**: Ningún trabajo de user story puede empezar hasta que esta fase esté completa

- [X] T003 [P] Agregar propiedades `ExpiredAt` (DateTime?) y `ExpirationReason` (string?) a la entidad `WaitlistOpportunity` en `crud_service/src/CrudService.Domain/Entities/WaitlistOpportunity.cs` y configurar el mapping EF Core en `crud_service/src/CrudService.Infrastructure/Persistence/CrudServiceDbContext.cs`
- [X] T004 [P] Agregar método `FindByIdAsync(long id)` a la interfaz `IWaitlistOpportunityRepository` en `crud_service/src/CrudService.Domain/Interfaces/IWaitlistOpportunityRepository.cs`
- [X] T005 Implementar `FindByIdAsync` en `WaitlistOpportunityRepository` en `crud_service/src/CrudService.Infrastructure/Persistence/Repositories/WaitlistOpportunityRepository.cs` con eager loading de `WaitlistEntry` (depende de T004)
- [X] T006 [P] Agregar método `OnOpportunityExpiredAsync(WaitlistOpportunity opportunity)` a la interfaz `IOpportunityObserver` en `crud_service/src/CrudService.Domain/Interfaces/IOpportunityObserver.cs`
- [X] T007 Implementar `OnOpportunityExpiredAsync` en `OpportunityActivatedObserver` en `crud_service/src/CrudService.Infrastructure/Messaging/OpportunityActivatedObserver.cs` publicando a RabbitMQ con routing key `waitlist.opportunity.expired`
- [X] T008 Implementar `OnOpportunityExpiredAsync` en `EmailNotificationObserver` en `crud_service/src/CrudService.Infrastructure/Messaging/EmailNotificationObserver.cs` (no-op o notificación de expiración según contrato existente)
- [X] T009 [P] Crear interfaz `IInventoryReturnPort` en `crud_service/src/CrudService.Domain/Interfaces/IInventoryReturnPort.cs` con método `ReturnToInventoryAsync(long ticketId, long eventId)`
- [X] T010 Crear adaptador `InventoryReturnAdapter` en `crud_service/src/CrudService.Infrastructure/Messaging/InventoryReturnAdapter.cs` que publique a RabbitMQ exchange `tickets` con routing key `ticket.returned_to_inventory`
- [X] T011 [P] Crear record `ExpireOpportunityCommand(long OpportunityId)` en `crud_service/src/CrudService.Application/UseCases/Waitlist/ExpireOpportunity/ExpireOpportunityCommand.cs`
- [X] T012 [P] Crear enum `ExpireOpportunityResultType` y record `ExpireOpportunityResult` en `crud_service/src/CrudService.Application/UseCases/Waitlist/ExpireOpportunity/ExpireOpportunityResult.cs`
- [X] T013 [P] Crear interfaz `IExpireOpportunityUseCase` en `crud_service/src/CrudService.Application/UseCases/Waitlist/ExpireOpportunity/IExpireOpportunityUseCase.cs`

**Checkpoint**: Todos los puertos, comandos y tipos están definidos. El handler y consumer se implementan en las fases de user story.

---

## Phase 3: User Story 1 — Expiración automática de oportunidad no reclamada (Priority: P1) 🎯 MVP

**Goal**: Cuando el TTL de una oportunidad activa vence, el sistema la marca como `expired` con motivo `ttl_expired` y marca de tiempo. Idempotente ante mensajes duplicados o tardíos.

**Independent Test**: Crear una oportunidad activa, invocar el handler con su ID, verificar transición a `expired` con `expired_at` y `expiration_reason` persistidos.

### Tests for User Story 1

> **RED primero: escribir tests, verificar que FALLAN, luego implementar**

- [X] T014 [P] [US1] Test unitario TC-HU6-01: oportunidad activa → expired con motivo `ttl_expired` y `expired_at` registrados en `crud_service/tests/CrudService.Application.Tests/Waitlist/ExpireOpportunityHandlerTests.cs`
- [X] T015 [P] [US1] Test unitario: oportunidad ya expirada → idempotente (retorna `AlreadyExpired`, ACK) en `crud_service/tests/CrudService.Application.Tests/Waitlist/ExpireOpportunityHandlerTests.cs`
- [X] T016 [P] [US1] Test unitario: oportunidad en estado `consumed` → idempotente (retorna `AlreadyConsumed`, ACK) en `crud_service/tests/CrudService.Application.Tests/Waitlist/ExpireOpportunityHandlerTests.cs`
- [X] T017 [P] [US1] Test unitario: oportunidad no encontrada → retorna `NotFound` en `crud_service/tests/CrudService.Application.Tests/Waitlist/ExpireOpportunityHandlerTests.cs`

### Implementation for User Story 1

- [X] T018 [US1] Implementar `ExpireOpportunityHandler` en `crud_service/src/CrudService.Application/UseCases/Waitlist/ExpireOpportunity/ExpireOpportunityHandler.cs`: cargar oportunidad por ID, validar estado `active` (idempotencia), transicionar a `expired` via `TransitionTo`, asignar `ExpiredAt` = UtcNow y `ExpirationReason` = `ttl_expired`, guardar via `UpdateAsync`, notificar observers via `OnOpportunityExpiredAsync`. Inyectar `IEnumerable<IOpportunityObserver>` (patrón existente de HU5).
- [X] T019 [US1] Registrar `IExpireOpportunityUseCase → ExpireOpportunityHandler` como scoped y `IInventoryReturnPort → InventoryReturnAdapter` como scoped en `crud_service/src/CrudService.Infrastructure/DependencyInjection.cs`
- [X] T020 [US1] Crear `WaitlistOpportunityExpiredConsumer` como `BackgroundService` en `crud_service/src/CrudService.Infrastructure/Messaging/WaitlistOpportunityExpiredConsumer.cs`: escuchar cola `q.waitlist.opportunity.expired`, deserializar `{opportunityId}`, crear scope DI, invocar `IExpireOpportunityUseCase.HandleAsync`. ACK en éxito/idempotencia, NACK con requeue:false en fallo técnico. Seguir patrón de `TicketReleasedConsumer`.
- [X] T021 [US1] Registrar `WaitlistOpportunityExpiredConsumer` como `HostedService` en `crud_service/src/CrudService.Infrastructure/DependencyInjection.cs`

**Checkpoint**: US1 funcional — oportunidades activas expiran automáticamente al recibir mensaje DLX. Tests GREEN.

---

## Phase 4: User Story 2 — Reasignación al siguiente comprador elegible (Priority: P2)

**Goal**: Tras expirar, el handler invoca `IAssignOpportunityUseCase` para reasignar al siguiente comprador FIFO. Si `Assigned`, la reasignación es exitosa.

**Independent Test**: Crear oportunidad activa + segundo comprador con inscripción activa. Expirar la oportunidad. Verificar que se invoca asignación y se crea nueva oportunidad para el siguiente comprador.

### Tests for User Story 2

- [X] T022 [P] [US2] Test unitario TC-HU6-02: tras expiración, handler invoca `IAssignOpportunityUseCase.HandleAsync` con ticketId y eventId correctos en `crud_service/tests/CrudService.Application.Tests/Waitlist/ExpireOpportunityHandlerTests.cs`
- [X] T023 [P] [US2] Test unitario: resultado `Assigned` → no publica retorno a inventario en `crud_service/tests/CrudService.Application.Tests/Waitlist/ExpireOpportunityHandlerTests.cs`

### Implementation for User Story 2

- [X] T024 [US2] Extender `ExpireOpportunityHandler` para invocar `IAssignOpportunityUseCase.HandleAsync(new AssignOpportunityCommand(opportunity.TicketId, opportunity.WaitlistEntry.EventId))` después de expirar y notificar observers en `crud_service/src/CrudService.Application/UseCases/Waitlist/ExpireOpportunity/ExpireOpportunityHandler.cs`

**Checkpoint**: US2 funcional — tras expiración, el siguiente comprador FIFO recibe una nueva oportunidad. Tests GREEN.

---

## Phase 5: User Story 3 — Devolución al inventario cuando no hay compradores elegibles (Priority: P3)

**Goal**: Si `IAssignOpportunityUseCase` retorna `NoEligible` o `AllFailed`, publicar `ticket.returned_to_inventory` para devolver la entrada al inventario.

**Independent Test**: Crear oportunidad activa sin más inscripciones activas. Expirar. Verificar publicación del evento de retorno a inventario.

### Tests for User Story 3

- [X] T025 [P] [US3] Test unitario TC-HU6-03: resultado `NoEligible` → invoca `IInventoryReturnPort.ReturnToInventoryAsync` en `crud_service/tests/CrudService.Application.Tests/Waitlist/ExpireOpportunityHandlerTests.cs`
- [X] T026 [P] [US3] Test unitario: resultado `AllFailed` → invoca `IInventoryReturnPort.ReturnToInventoryAsync` en `crud_service/tests/CrudService.Application.Tests/Waitlist/ExpireOpportunityHandlerTests.cs`
- [X] T027 [P] [US3] Test unitario TC-HU6-04: tras expiración, la inscripción del comprador permanece en estado ya asignado (no se modifica `WaitlistEntry.Status`) en `crud_service/tests/CrudService.Application.Tests/Waitlist/ExpireOpportunityHandlerTests.cs`
- [X] T028 [P] [US3] Test unitario TC-HU6-05: fallo técnico en `IInventoryReturnPort` → la oportunidad ya está expirada (estado consistente), se registra log Error y se incrementa contador en `crud_service/tests/CrudService.Application.Tests/Waitlist/ExpireOpportunityHandlerTests.cs`

### Implementation for User Story 3

- [X] T029 [US3] Extender `ExpireOpportunityHandler` para evaluar `AssignOpportunityResult.Type`: si `NoEligible` o `AllFailed`, invocar `IInventoryReturnPort.ReturnToInventoryAsync(ticketId, eventId)` en `crud_service/src/CrudService.Application/UseCases/Waitlist/ExpireOpportunity/ExpireOpportunityHandler.cs`
- [X] T030 [US3] Agregar logging estructurado (Error level) y contadores de métricas `expiration_reassignment_failures_total` y `expiration_inventory_return_failures_total` al handler ante fallos técnicos en `crud_service/src/CrudService.Application/UseCases/Waitlist/ExpireOpportunity/ExpireOpportunityHandler.cs`

**Checkpoint**: US3 funcional — entradas sin compradores elegibles vuelven al inventario. Fallos técnicos son observables. Tests GREEN.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Tests de integración, validación end-to-end, documentación

- [X] T031 [P] Test de integración con Testcontainers: verificar transición `active → expired` con `expired_at` y `expiration_reason` persistidos en BD en `crud_service/tests/CrudService.Infrastructure.Tests/Integration/WaitlistOpportunityExpirationTests.cs`
- [X] T031b [P] Test unitario: verificar que `InventoryReturnAdapter` publica con los tres campos (`ticketId`, `eventId`, `returnedAt`) en `crud_service/tests/CrudService.Infrastructure.Tests/Messaging/InventoryReturnAdapterTests.cs`
- [X] T031c Test de integración: verificar ciclo completo expiración → reasignación → segunda expiración → retorno a inventario (si no hay más elegibles) en `crud_service/tests/CrudService.Infrastructure.Tests/Integration/WaitlistOpportunityExpirationTests.cs`
- [X] T032 [P] Test de integración con Testcontainers: verificar `FindByIdAsync` con eager loading de `WaitlistEntry` en `crud_service/tests/CrudService.Infrastructure.Tests/Integration/WaitlistOpportunityExpirationTests.cs`
- [X] T033 Ejecutar `dotnet test crud_service/CrudService.sln` y verificar que TODOS los tests (existentes + nuevos) pasan GREEN
- [X] T034 Ejecutar validación quickstart.md: `docker compose up -d --build` y verificar que el consumer arranca sin errores en logs

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sin dependencias — puede iniciar inmediatamente
- **Foundational (Phase 2)**: Depende de Setup — **BLOQUEA** todas las user stories
- **US1 (Phase 3)**: Depende de Foundational (Phase 2)
- **US2 (Phase 4)**: Depende de US1 (Phase 3) — extiende el handler
- **US3 (Phase 5)**: Depende de US2 (Phase 4) — extiende el handler con evaluación de resultado
- **Polish (Phase 6)**: Depende de US3 (Phase 5) — tests de integración cubren todo el flujo

### Within Each User Story

- Tests DEBEN escribirse y FALLAR (RED) antes de implementación
- Implementación hace pasar los tests (GREEN)
- Refactor opcional si los tests siguen GREEN
- Commits TDD: `test(red):`, `feat(green):`, `refactor:`

### Parallel Opportunities

**Phase 2 (Foundational)**: T003, T004, T006, T009, T011, T012, T013 pueden ejecutarse en paralelo (archivos distintos sin dependencias cruzadas)

**Phase 3 (US1) Tests**: T014, T015, T016, T017 pueden escribirse en paralelo (mismo archivo pero tests independientes)

**Phase 5 (US3) Tests**: T025, T026, T027, T028 pueden escribirse en paralelo

**Phase 6 (Polish)**: T031, T032 pueden ejecutarse en paralelo

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Paralelo — 7 tareas en archivos distintos:
Task T003: Agregar ExpiredAt/ExpirationReason a WaitlistOpportunity.cs + DbContext
Task T004: Agregar FindByIdAsync a IWaitlistOpportunityRepository.cs
Task T006: Agregar OnOpportunityExpiredAsync a IOpportunityObserver.cs
Task T009: Crear IInventoryReturnPort.cs
Task T011: Crear ExpireOpportunityCommand.cs
Task T012: Crear ExpireOpportunityResult.cs
Task T013: Crear IExpireOpportunityUseCase.cs

# Secuencial — dependen de los anteriores:
Task T005: Implementar FindByIdAsync (depende de T004)
Task T007: Implementar OnOpportunityExpiredAsync en OpportunityActivatedObserver (depende de T006)
Task T008: Implementar OnOpportunityExpiredAsync en EmailNotificationObserver (depende de T006)
Task T010: Crear InventoryReturnAdapter (depende de T009)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001, T002)
2. Complete Phase 2: Foundational (T003–T013)
3. Complete Phase 3: US1 — Tests RED (T014–T017) → Handler GREEN (T018–T021)
4. **STOP and VALIDATE**: `dotnet test` — US1 tests pasan, oportunidades expiran correctamente
5. Commit: `test(red): add expiration handler tests` → `feat(green): implement opportunity expiration handler and consumer`

### Incremental Delivery

1. Setup + Foundational → puertos y tipos definidos
2. US1 → oportunidades expiran (MVP!)
3. US2 → reasignación post-expiración
4. US3 → retorno a inventario + observabilidad
5. Cada story extiende el handler sin romper stories anteriores

### Commit Sequence (TDD)

```
test(red): add opportunity expiration handler unit tests
feat(green): implement opportunity expiration handler and DLX consumer
test(red): add reassignment post-expiration unit tests
feat(green): implement reassignment via IAssignOpportunityUseCase in expiration handler
test(red): add inventory return and observability unit tests
feat(green): implement inventory return and structured error logging in expiration handler
test(green): add integration tests for opportunity expiration with Testcontainers
```
