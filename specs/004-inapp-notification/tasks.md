# Tasks: Notificación In-App SSE de Lista de Espera

**Input**: Design documents from `/specs/004-inapp-notification/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/sse.md, quickstart.md

**Tests**: Incluidos — la constitución del proyecto exige TDD estricto (principio III).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Crear la estructura de carpetas y las interfaces ISP que serán consumidas por todas las historias de usuario.

- [X] T001 Create `crud_service/src/CrudService.Infrastructure/Sse/` directory structure
- [X] T002 [P] Create `IWaitlistSseNotifier` interface in `crud_service/src/CrudService.Infrastructure/Sse/IWaitlistSseNotifier.cs` with `SendEventAsync(string email, string eventType, string jsonPayload)`
- [X] T003 [P] Create `IWaitlistSseSubscriber` interface in `crud_service/src/CrudService.Infrastructure/Sse/IWaitlistSseSubscriber.cs` with `RegisterAsync`, `UnregisterAsync`, `GetConnectionCount`
- [X] T004 [P] Create `SseEvent` DTO in `crud_service/src/CrudService.Infrastructure/Sse/SseEvent.cs` with `EventType` and `Data` properties
- [X] T005 [P] Create `SseClient` model in `crud_service/src/CrudService.Infrastructure/Sse/SseClient.cs` with `Id`, `Email`, `ResponseStream` (HttpResponse), `EventChannel`, `CancellationToken`, `ConnectedAt` properties

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Implementar el hub SSE singleton y registrarlo en DI. Sin este componente, ninguna historia de usuario puede emitir ni recibir eventos SSE.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Tests

- [X] T006 [P] RED: Create `WaitlistSseHubTests.cs` in `crud_service/tests/CrudService.Infrastructure.Tests/Sse/WaitlistSseHubTests.cs` — test `RegisterAsync` returns `SseClient` with correct email and unique Id
- [X] T007 [P] RED: Add test in `WaitlistSseHubTests.cs` — test `UnregisterAsync` removes client; `GetConnectionCount` returns 0 after unregister
- [X] T008 [P] RED: Add test in `WaitlistSseHubTests.cs` — test `SendEventAsync` writes `SseEvent` to `Channel<SseEvent>` of matching email client
- [X] T009 [P] RED: Add test in `WaitlistSseHubTests.cs` — test `SendEventAsync` with non-matching email does not write to unrelated clients
- [X] T010 [P] RED: Add test in `WaitlistSseHubTests.cs` — test `GetConnectionCount` returns correct count for multiple clients on same email

### Implementation

- [X] T011 GREEN: Implement `WaitlistSseHub` in `crud_service/src/CrudService.Infrastructure/Sse/WaitlistSseHub.cs` — `ConcurrentDictionary<string, ConcurrentBag<SseClient>>`, implements `IWaitlistSseNotifier` and `IWaitlistSseSubscriber`; pass tests T006–T010
- [X] T012 GREEN: Register `WaitlistSseHub` as singleton in `crud_service/src/CrudService.Infrastructure/DependencyInjection.cs` — bind `IWaitlistSseNotifier` and `IWaitlistSseSubscriber` to same instance (ISP pattern from `TicketStatusHub`)
- [X] T013 REFACTOR: Review `WaitlistSseHub` for thread-safety edge cases; ensure `ConcurrentBag` cleanup on `UnregisterAsync` handles empty bags correctly

**Checkpoint**: Foundation ready — hub SSE registrado y testeado. Las historias de usuario pueden comenzar.

---

## Phase 3: User Story 1 — Recepción de notificación de oportunidad activa en tiempo real (Priority: P1) 🎯 MVP

**Goal**: El comprador conectado al stream SSE recibe el evento `opportunity_activated` con payload completo cuando el sistema activa una oportunidad para él.

**Independent Test**: Conectar un cliente SSE con un correo, publicar `waitlist.opportunity.activated` a RabbitMQ, verificar que el cliente recibe `opportunity_activated` con los 5 campos obligatorios.

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T014 [P] [US1] RED: Create `SseNotificationConsumerTests.cs` in `crud_service/tests/CrudService.Infrastructure.Tests/Sse/SseNotificationConsumerTests.cs` — test consumer calls `IWaitlistSseNotifier.SendEventAsync` with correct email, event type `opportunity_activated`, and JSON payload containing `opportunityId`, `ticketId`, `eventId`, `expiresAt`, `remainingMinutes` when receiving a valid `waitlist.opportunity.activated` message
- [X] T015 [P] [US1] RED: Add test in `SseNotificationConsumerTests.cs` — test consumer correctly calculates `remainingMinutes` as floor of difference between `expiresAt` and now (FR-005)
- [X] T016 [P] [US1] RED: Add test in `SseNotificationConsumerTests.cs` — test consumer handles malformed message gracefully: logs error and ACKs (does not crash)
- [X] T017 [P] [US1] RED: Create `WaitlistStreamEndpointTests.cs` in `crud_service/tests/CrudService.Api.Tests/Waitlist/WaitlistStreamEndpointTests.cs` — test `StreamSse` action sets response headers `Content-Type: text/event-stream`, `Cache-Control: no-cache`, `Connection: keep-alive` and calls `IWaitlistSseSubscriber.RegisterAsync`
- [X] T018 [P] [US1] RED: Add test in `WaitlistStreamEndpointTests.cs` — test `StreamSse` action returns `400 Bad Request` when email is missing or invalid format (FR-012)
- [X] T019 [P] [US1] RED: Add test in `WaitlistStreamEndpointTests.cs` — test `StreamSse` action returns `429 Too Many Requests` when `GetConnectionCount` equals `SSE_MAX_CONNECTIONS_PER_EMAIL` (FR-013)
- [X] T019b [P] [US1] RED: Add test in `WaitlistStreamEndpointTests.cs` — test `StreamSse` action emits `: keepalive\n\n` comment at configured interval (`SSE_KEEPALIVE_INTERVAL_SECONDS`) while connection is open (FR-009)

### Implementation for User Story 1

- [X] T020 [US1] GREEN: Implement `SseNotificationConsumer` as `BackgroundService` in `crud_service/src/CrudService.Infrastructure/Messaging/SseNotificationConsumer.cs` — connect to RabbitMQ, declare auto-delete exclusive queue `q.waitlist.sse.{Guid}` bound to exchange `tickets` with routing key `waitlist.opportunity.activated`, deserialize payload, extract `buyerEmail`, construct SSE payload (opportunityId, ticketId, eventId, expiresAt, remainingMinutes), call `IWaitlistSseNotifier.SendEventAsync`; pass tests T014–T016
- [X] T021 [US1] GREEN: Add `StreamSse` action to `WaitlistController` in `crud_service/src/CrudService.Api/Controllers/WaitlistController.cs` — validate email (reuse existing `IsValidEmail`), check connection limit via `IWaitlistSseSubscriber.GetConnectionCount`, set SSE headers, register client via `RegisterAsync`, loop reading from `SseClient.EventChannel` writing SSE-formatted events to response, handle keep-alive via periodic `: keepalive\n\n` comments, unregister on `RequestAborted`; pass tests T017–T019b
- [X] T022 [US1] GREEN: Register `SseNotificationConsumer` as `AddHostedService` in `crud_service/src/CrudService.Api/Program.cs`
- [X] T023 [US1] GREEN: Add `SSE_KEEPALIVE_INTERVAL_SECONDS` and `SSE_MAX_CONNECTIONS_PER_EMAIL` environment variable reading in `Program.cs` or `DependencyInjection.cs` (IConfiguration binding or `Environment.GetEnvironmentVariable`)
- [X] T024 [US1] REFACTOR: Extract SSE format writing (`event: {type}\ndata: {json}\n\n` and `: keepalive\n\n`) into private helper method in `WaitlistController` or static utility for reuse

**Checkpoint**: US1 completamente funcional — el comprador conectado al stream SSE recibe `opportunity_activated` en tiempo real. Se puede verificar con curl + publicación a RabbitMQ.

---

## Phase 4: User Story 2 — Recepción de notificación de oportunidad expirada en tiempo real (Priority: P2)

**Goal**: El comprador conectado al stream SSE recibe el evento `opportunity_expired` cuando su oportunidad expira.

**Independent Test**: Conectar un cliente SSE, simular expiración de oportunidad publicando el evento correspondiente, verificar que recibe `opportunity_expired` con `opportunityId`, `eventId` y `reason`.

### Tests for User Story 2

- [X] T025 [P] [US2] RED: Add test in `SseNotificationConsumerTests.cs` — test consumer calls `IWaitlistSseNotifier.SendEventAsync` with event type `opportunity_expired` and JSON payload containing `opportunityId`, `eventId`, `reason` when receiving a `waitlist.opportunity.expired` message

### Implementation for User Story 2

- [X] T026 [US2] GREEN: Extend `SseNotificationConsumer` in `crud_service/src/CrudService.Infrastructure/Messaging/SseNotificationConsumer.cs` — add second queue binding for routing key `waitlist.opportunity.expired` (or handle both routing keys in the same consumer), construct `opportunity_expired` payload and dispatch via `IWaitlistSseNotifier.SendEventAsync`; pass test T025
- [X] T027 [US2] REFACTOR: Review consumer for DRY — ensure activated/expired message handling shares common deserialization and dispatching logic without duplication

**Checkpoint**: US1 + US2 funcionales — el comprador recibe tanto `opportunity_activated` como `opportunity_expired` en tiempo real.

---

## Phase 5: User Story 3 — Consulta posterior consistente con el estado real (Priority: P2)

**Goal**: El comprador que navega fuera y regresa obtiene estado coherente mediante la consulta existente (`GET /api/waitlist/entries`).

**Independent Test**: Activar una oportunidad, verificar que `GET /api/waitlist/entries?eventId=42&email=...` devuelve la oportunidad activa con `remainingMinutes` actualizado.

### Tests for User Story 3

- [X] T028 [US3] RED: Add test in `WaitlistStreamEndpointTests.cs` — test that on SSE reconnection (new `RegisterAsync` call), the connection is established successfully without replaying past events (FR-011)

### Implementation for User Story 3

- [X] T029 [US3] GREEN: Verify existing `GET /api/waitlist/entries` endpoint in `crud_service/src/CrudService.Api/Controllers/WaitlistController.cs` already returns `remainingMinutes` via `GetWaitlistStatusQueryHandler` — if not, ensure the handler computes `remainingMinutes = floor((expiresAt - now).TotalMinutes)` for active opportunities; pass test T028
- [X] T030 [US3] REFACTOR: Ensure reconnection flow (unregister old client + register new client on same email) is clean; no stale `SseClient` entries remain in hub

**Checkpoint**: US1 + US2 + US3 funcionales — estado siempre coherente para el comprador, conectado o no.

---

## Phase 6: User Story 4 — La capa de notificación no modifica el estado de la oportunidad (Priority: P3)

**Goal**: Garantizar que la capa SSE es exclusivamente de lectura; ningún fallo de notificación afecta al dominio.

**Independent Test**: Invocar el consumer y verificar que no se realizan operaciones de escritura sobre entidades de dominio.

### Tests for User Story 4

- [X] T031 [P] [US4] RED: Add test in `SseNotificationConsumerTests.cs` — test consumer does NOT inject or call any repository write method (no `IWaitlistEntryRepository`, `IWaitlistOpportunityRepository` dependencies); only `IWaitlistSseNotifier` (FR-006)
- [X] T032 [P] [US4] RED: Add test in `SseNotificationConsumerTests.cs` — test consumer catches `Exception` on `SendEventAsync` failure, logs the error, and does NOT rethrow or NACK the message (FR-007)

### Implementation for User Story 4

- [X] T033 [US4] GREEN: Add try/catch in `SseNotificationConsumer.SendEventAsync` dispatch in `crud_service/src/CrudService.Infrastructure/Messaging/SseNotificationConsumer.cs` — catch exceptions, log with `ILogger.LogError`, ACK the message regardless; pass tests T031–T032
- [X] T034 [US4] GREEN: Add try/catch in `WaitlistSseHub.SendEventAsync` in `crud_service/src/CrudService.Infrastructure/Sse/WaitlistSseHub.cs` — if writing to a client's channel fails, log and remove stale client; do not propagate exception to caller

**Checkpoint**: Todas las US funcionales — la capa de notificación es robusta y read-only.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validación final, variables de entorno en compose, y verificación quickstart.

- [X] T035 [P] Add `SSE_KEEPALIVE_INTERVAL_SECONDS` and `SSE_MAX_CONNECTIONS_PER_EMAIL` environment variables to `compose.yml` for `crud-service` container with default values
- [X] T036 Run all tests: `dotnet test crud_service/tests/CrudService.Infrastructure.Tests --filter "Sse"` and `dotnet test crud_service/tests/CrudService.Api.Tests --filter "Waitlist"` — verify all pass
- [X] T037 Run quickstart.md validation steps (curl SSE endpoint, verify keep-alive, trigger opportunity_activated, verify 400/429)
- [X] T038 [P] Verify no compile warnings or errors across all modified projects: `dotnet build crud_service/CrudService.sln --no-restore`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion (T001–T005) — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational phase completion (T011–T013)
- **User Story 2 (Phase 4)**: Depends on US1 implementation (T020 — consumer exists to extend)
- **User Story 3 (Phase 5)**: Depends on Foundational phase completion (T011–T013) — can run in parallel with US1
- **User Story 4 (Phase 6)**: Depends on US1 implementation (T020 — consumer exists to add error handling)
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Phase 2 — no dependencies on other stories
- **User Story 2 (P2)**: Depends on US1 (extends `SseNotificationConsumer` created in T020)
- **User Story 3 (P2)**: Can start after Phase 2 — independent of US1/US2 (uses existing endpoint)
- **User Story 4 (P3)**: Depends on US1 (adds error handling to `SseNotificationConsumer` from T020)

### Within Each User Story

- Tests MUST be written and FAIL before implementation (TDD RED → GREEN → REFACTOR)
- Interfaces/DTOs before hub implementation
- Hub before consumer
- Consumer before endpoint
- Core implementation before integration

### Parallel Opportunities

Within Phase 1:
```
T002 (IWaitlistSseNotifier) ─┐
T003 (IWaitlistSseSubscriber) ├── All in parallel (different files)
T004 (SseEvent DTO) ──────────┤
T005 (SseClient model) ───────┘
```

Within Phase 2 (tests):
```
T006 (Register test) ──────┐
T007 (Unregister test) ────├── All in parallel (same file, different tests)
T008 (SendEvent test) ─────┤
T009 (Non-matching test) ──┤
T010 (Count test) ─────────┘
```

Within Phase 3 (tests):
```
T014 (Consumer activated test) ──┐
T015 (remainingMinutes test) ────├── In parallel (different test files)
T016 (Malformed message test) ───┘
T017 (SSE headers test) ────────┐
T018 (Email validation test) ───├── In parallel (different test file from above)
T019 (429 limit test) ──────────┤
T019b (Keep-alive test) ────────┘
```

After Foundational completes:
```
US1 (Phase 3) ──── Must complete first (creates consumer)
       ├── US2 (Phase 4) ──── Extends consumer
       └── US4 (Phase 6) ──── Adds error handling to consumer
US3 (Phase 5) ──── Can run in parallel with US1 (independent)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T005)
2. Complete Phase 2: Foundational — hub SSE testeado (T006–T013)
3. Complete Phase 3: User Story 1 — consumer + endpoint SSE (T014–T024)
4. **STOP and VALIDATE**: Verify con curl que el stream SSE entrega `opportunity_activated`
5. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → Hub SSE listo y testeado
2. User Story 1 → `opportunity_activated` funcional → **MVP listo**
3. User Story 3 → Consulta posterior consistente (independiente, puede ir antes de US2)
4. User Story 2 → `opportunity_expired` funcional → Ciclo de vida visible completo
5. User Story 4 → Robustez read-only → Feature completa
6. Polish → Variables de entorno en compose, quickstart validado

### Suggested MVP Scope

**Solo User Story 1 (Phase 1 + Phase 2 + Phase 3)**: 24 tareas (T001–T024) entregan el valor principal de la feature: notificación en tiempo real de oportunidad activada. Las historias restantes (US2–US4) son complementarias y pueden entregarse incrementalmente.

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- TDD cycle: RED (test fails) → GREEN (minimal implementation) → REFACTOR
- Commit after each TDD cycle with convention: `test(red): ...`, `feat(green): ...`, `refactor: ...`
- Stop at any checkpoint to validate story independently
- `opportunity_expired` (US2) está preparada infraestructuralmente pero se emitirá cuando HU6 (expiración) esté implementada
