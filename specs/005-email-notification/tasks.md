# Tasks: Notificación por Correo Electrónico de Oportunidad de Lista de Espera

**Input**: Design documents from `/specs/005-email-notification/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, quickstart.md

**Tests**: Incluidos — TDD estricto obligatorio (Constitution III). Tests definidos: TC-HU5-01..04.

**Organization**: Tareas agrupadas por user story. US1 y US2 son P1 (se implementan juntas porque el observer envía correo Y audita en la misma operación). US3 es P2 (aislamiento validado por tests de US1/US2 más tests dedicados).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 0: Corrección de arquitectura hexagonal (transversal — bloquea Phase 1)

**Purpose**: Corregir violaciones de la arquitectura hexagonal con DDD definida en `docs/Features/Feature1/Planning2.md`. Estas violaciones existen desde HUs anteriores y DEBEN resolverse antes de agregar código nuevo que las perpetúe.

**Regla de dependencia (Planning2.md)**: Domain no referencia a nadie. Application solo referencia Domain. Infrastructure referencia Domain y Application. Api referencia todos para Composition Root pero su código propio solo delega a puertos.

### Violación 1 — CRÍTICA: Domain depende de Npgsql (framework de BD)

`CrudService.Domain.csproj` tiene `PackageReference Include="Npgsql"`. Los enums de Domain usan `[PgName("...")]` de `NpgsqlTypes`, acoplando el núcleo de negocio a PostgreSQL. Esto viola la regla: "Domain no referencia a nadie" y "entidades de dominio puras, sin dependencias de framework".

- [X] T-ARCH-01 Eliminar dependencia `Npgsql` de `crud_service/src/CrudService.Domain/CrudService.Domain.csproj`. Eliminar atributos `[PgName("...")]` y `using NpgsqlTypes` de todos los enums en `crud_service/src/CrudService.Domain/Enums/` y entidades en `crud_service/src/CrudService.Domain/Entities/` que los usen (`Ticket.cs`, `Payment.cs`, `WaitlistEntryStatus.cs`, `WaitlistOpportunityStatus.cs`)
- [X] T-ARCH-02 Mover el mapeo de valores de enum PostgreSQL a `TicketingDbContext.OnModelCreating` en `crud_service/src/CrudService.Infrastructure/Persistence/TicketingDbContext.cs`, usando `HasConversion` o la API de Npgsql EF Core para mapear cada valor de enum C# (PascalCase) al nombre PostgreSQL (snake_case) sin necesidad de `[PgName]` en Domain
- [X] T-ARCH-03 Verificar que `dotnet build crud_service/CrudService.sln` compila sin errores y que `dotnet test` pasa tras la eliminación de `[PgName]`

### Violación 2 — CRÍTICA: Api importa interfaces definidas en Infrastructure

`WaitlistController.cs` usa `CrudService.Infrastructure.Sse.IWaitlistSseSubscriber` y `TicketsController.cs` usa `CrudService.Infrastructure.Messaging.ITicketStatusSubscriber`. Las interfaces que consume Api deben estar en Application o Domain, no en Infrastructure.

- [X] T-ARCH-04 [P] Mover `IWaitlistSseSubscriber` y `IWaitlistSseNotifier` a `crud_service/src/CrudService.Application/Interfaces/` (o `Domain/Interfaces/`). Actualizar los `using` en `WaitlistController.cs`, `WaitlistSseHub.cs`, `SseNotificationConsumer.cs`, y `DependencyInjection.cs`
- [X] T-ARCH-05 [P] Mover `ITicketStatusSubscriber` y `ITicketStatusNotifier` a `crud_service/src/CrudService.Application/Interfaces/` (o `Domain/Interfaces/`). Actualizar los `using` en `TicketsController.cs`, `TicketStatusHub.cs`, `TicketStatusConsumer.cs`, y `DependencyInjection.cs`
- [X] T-ARCH-06 Verificar que ningún archivo en `crud_service/src/CrudService.Api/` tiene `using CrudService.Infrastructure.*` (excepto en `Program.cs` para Composition Root). Ejecutar `dotnet build` y `dotnet test`

**Checkpoint**: Compilación y tests verdes. Domain no tiene dependencias de framework. Api no importa interfaces de Infrastructure. La arquitectura hexagonal cumple con Planning2.md.

---

## Phase 1: Setup

**Purpose**: Agregar DDL y artefactos de dominio nuevos que no dependen de lógica de negocio

- [X] T001 Agregar DDL de `notification_delivery_status` enum y tabla `notification_deliveries` en `scripts/schema.sql`
- [X] T002 [P] Crear enum `NotificationDeliveryStatus` (sin `[PgName]` — mapeo en Infrastructure via `HasConversion`, alineado con Phase 0) en `crud_service/src/CrudService.Domain/Enums/NotificationDeliveryStatus.cs`
- [X] T003 [P] Crear record `OpportunityActivatedEvent` en `crud_service/src/CrudService.Domain/Events/OpportunityActivatedEvent.cs`
- [X] T004 [P] Crear entidad `NotificationDelivery` en `crud_service/src/CrudService.Domain/Entities/NotificationDelivery.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Refactor de la interfaz Observer (HU3) y puertos nuevos. DEBE completarse antes de las user stories.

**⚠️ CRITICAL**: La interfaz `IOpportunityObserver` cambia de firma y el handler pasa de inyección singular a `IEnumerable`. Todos los tests de HU3 deben seguir pasando tras este refactor.

### Tests del refactor

- [X] T005 test(red): Actualizar tests de `AssignOpportunityHandler` para inyectar `IEnumerable<IOpportunityObserver>` y verificar que el observer recibe `OpportunityActivatedEvent` (en vez de `WaitlistOpportunity`) en `crud_service/tests/CrudService.Application.Tests/Waitlist/AssignOpportunityHandlerTests.cs`

### Implementación del refactor

- [X] T006 feat(green): Modificar `IOpportunityObserver.OnOpportunityActivatedAsync` para recibir `OpportunityActivatedEvent` en vez de `WaitlistOpportunity` en `crud_service/src/CrudService.Domain/Interfaces/IOpportunityObserver.cs`
- [X] T007 feat(green): Modificar `AssignOpportunityHandler` para inyectar `IEnumerable<IOpportunityObserver>`, construir `OpportunityActivatedEvent` con `EventName` resuelto, e iterar observers con foreach en `crud_service/src/CrudService.Application/UseCases/Waitlist/AssignOpportunity/AssignOpportunityHandler.cs`
- [X] T008 feat(green): Adaptar `OpportunityActivatedObserver` para recibir `OpportunityActivatedEvent` y eliminar el DTO `OpportunityActivatedEvent` de Infrastructure/Messaging (el record del Domain lo reemplaza) en `crud_service/src/CrudService.Infrastructure/Messaging/OpportunityActivatedObserver.cs`
- [X] T009 feat(green): Actualizar registro DI: cambiar `AddScoped<IOpportunityObserver, OpportunityActivatedObserver>()` para ser compatible con `IEnumerable` en `crud_service/src/CrudService.Infrastructure/DependencyInjection.cs`

### Puertos nuevos

- [X] T010 [P] Crear record `EmailSendResult` y la interfaz `IEmailSender` con método `SendOpportunityNotificationAsync` que retorna `Task<EmailSendResult>` en `crud_service/src/CrudService.Domain/Interfaces/IEmailSender.cs`
- [X] T011 [P] Crear interfaz `INotificationDeliveryRepository` con métodos `AddAsync` y `UpdateAsync` en `crud_service/src/CrudService.Domain/Interfaces/INotificationDeliveryRepository.cs`

### Infrastructure base

- [X] T012 Registrar `HasPostgresEnum<NotificationDeliveryStatus>`, agregar `DbSet<NotificationDelivery>`, y configurar mapeo de columnas en `crud_service/src/CrudService.Infrastructure/Persistence/TicketingDbContext.cs`
- [X] T013 Implementar `NotificationDeliveryRepository` con EF Core en `crud_service/src/CrudService.Infrastructure/Persistence/Repositories/NotificationDeliveryRepository.cs`
- [X] T014 Implementar `LogEmailSender` (stub log-only para MVP) con timeout configurable via `EMAIL_SENDER_TIMEOUT_MS` en `crud_service/src/CrudService.Infrastructure/Services/LogEmailSender.cs`

**Checkpoint**: Compilación exitosa. Los 7 tests originales de `AssignOpportunityHandlerTests` pasan con la nueva firma. Puertos e infraestructura base listos.

---

## Phase 3: User Story 1 + User Story 2 — Envío de correo con auditoría (Priority: P1) 🎯 MVP

**Goal**: Al activarse una oportunidad, `EmailNotificationObserver` envía un correo con el contenido obligatorio (FR-001, FR-002, FR-003, FR-008, FR-009) y registra cada intento en `notification_deliveries` (FR-004, FR-005), todo aislado del ciclo de vida de la oportunidad (FR-006, FR-007).

**Independent Test**: Activar una oportunidad para un comprador con correo válido → se genera correo con contenido correcto → registro de auditoría `sent` en `notification_deliveries`. Simular fallo del proveedor → registro de auditoría `failed` con `failure_reason`.

**US1 y US2 se implementan juntas**: El observer es una única clase (`EmailNotificationObserver`) que ejecuta envío + auditoría en la misma operación. Separarlos en fases distintas sería artificial.

### Tests (RED — escribir primero, verificar que fallan)

- [X] T015 [P] [US1] test(red): TC-HU5-01 — Correo enviado al activarse la oportunidad: `EmailNotificationObserver.OnOpportunityActivatedAsync` invoca `IEmailSender.SendOpportunityNotificationAsync` con `buyerEmail`, `eventName`, `expiresAt` del record en `crud_service/tests/CrudService.Infrastructure.Tests/Waitlist/EmailNotificationObserverTests.cs`
- [X] T016 [P] [US1] test(red): TC-HU5-02 — Contenido mínimo completo (FR-002, 5 elementos): verificar que `IEmailSender.SendOpportunityNotificationAsync` recibe los 5 elementos obligatorios: (1) `eventName` sanitizado (sin HTML), (2) indicación de reserva temporal, (3) vigencia de 15 minutos (`expiresAt`), (4) instrucción de verificar en la aplicación, (5) disclaimer de aviso informativo. También recibe `buyerEmail` del record en `crud_service/tests/CrudService.Infrastructure.Tests/Waitlist/EmailNotificationObserverTests.cs`
- [X] T016b [P] [US1] test(red): FR-008 — Correo sin enlaces de acción directa: verificar que los argumentos pasados a `IEmailSender.SendOpportunityNotificationAsync` no contienen URLs de pago ni confirmación; el correo solo instruye a consultar la aplicación en `crud_service/tests/CrudService.Infrastructure.Tests/Waitlist/EmailNotificationObserverTests.cs`
- [X] T017 [P] [US2] test(red): Envío exitoso registra `status = sent` en `notification_deliveries`: `EmailNotificationObserver` crea un `NotificationDelivery` con `status = Pending`, invoca `IEmailSender`, y actualiza a `Sent` con `sent_at` en `crud_service/tests/CrudService.Infrastructure.Tests/Waitlist/EmailNotificationObserverTests.cs`
- [X] T018 [P] [US2] test(red): Fallo del proveedor registra `status = failed` con `failure_reason`: al retornar `EmailSendResult(false, "provider error")` desde `IEmailSender`, el observer crea el registro `Pending`, lo actualiza a `Failed` con `FailureReason` = razón del result en `crud_service/tests/CrudService.Infrastructure.Tests/Waitlist/EmailNotificationObserverTests.cs`
- [X] T018b [P] [US2] test(red): Timeout del proveedor registra `status = failed` con `failure_reason = "timeout"` (FR-010): al retornar `EmailSendResult(false, "timeout")` desde `IEmailSender`, el observer actualiza a `Failed` con `FailureReason = "timeout"` en `crud_service/tests/CrudService.Infrastructure.Tests/Waitlist/EmailNotificationObserverTests.cs`
- [X] T018c [P] [US2] test(red): FR-005 — Inmutabilidad post-terminal: verificar que un `NotificationDelivery` con `status = Sent` (o `Failed`) rechaza una segunda actualización de estado. La entidad o el repositorio DEBE impedir transiciones desde un estado terminal en `crud_service/tests/CrudService.Infrastructure.Tests/Waitlist/EmailNotificationObserverTests.cs`

### Implementación (GREEN — mínimo código para pasar los tests)

- [X] T019 [US1] feat(green): Implementar `EmailNotificationObserver` con `IOpportunityObserver`, `IEmailSender`, `INotificationDeliveryRepository`, `ILogger`. En `OnOpportunityActivatedAsync`: crear `NotificationDelivery(Pending)` → guardar → invocar `IEmailSender` → inspeccionar `EmailSendResult` → si `Success`: actualizar a `Sent`; si `!Success`: actualizar a `Failed` con `FailureReason` del result → guardar. Try-catch externo: captura cualquier excepción inesperada y loguea sin propagar en `crud_service/src/CrudService.Infrastructure/Services/EmailNotificationObserver.cs`
- [X] T020 [US1] feat(green): Registrar `EmailNotificationObserver` como segundo `IOpportunityObserver` y los adaptadores `IEmailSender → LogEmailSender`, `INotificationDeliveryRepository → NotificationDeliveryRepository` en `crud_service/src/CrudService.Infrastructure/DependencyInjection.cs`

### Refactor

- [X] T021 refactor: Revisar y limpiar `EmailNotificationObserver` y tests — nombres intencionales, sin código muerto, sin duplicación entre tests

**Checkpoint**: Los 4 tests TC-HU5-01..02 + auditoría éxito/fallo pasan. `dotnet test CrudService.Infrastructure.Tests` verde. El observer se invoca automáticamente al activar una oportunidad. El correo se loguea (MVP stub) y el intento queda auditado.

---

## Phase 4: User Story 3 — Aislamiento del canal de correo (Priority: P2)

**Goal**: Verificar formalmente que un fallo en el envío de correo nunca afecta la oportunidad ni propaga excepciones al handler de asignación (FR-006, FR-007).

**Independent Test**: Configurar `IEmailSender` para fallar sistemáticamente, activar una oportunidad, verificar que la oportunidad permanece activa y el fallo queda auditado.

### Tests (RED)

- [X] T022 [US3] test(red): TC-HU5-03 — Fallo del proveedor de correo no afecta la oportunidad: `EmailNotificationObserver.OnOpportunityActivatedAsync` NO lanza excepción cuando `IEmailSender` retorna `EmailSendResult(false, ...)`, registra `Failed` en `notification_deliveries`, y el caller (handler) no se ve afectado en `crud_service/tests/CrudService.Infrastructure.Tests/Waitlist/EmailNotificationObserverTests.cs`
- [X] T022b [US3] test(red): Excepción inesperada de `IEmailSender` no propaga: si `SendOpportunityNotificationAsync` lanza excepción (en vez de retornar result), el observer la captura, registra `Failed` con `FailureReason = ex.Message`, y no propaga en `crud_service/tests/CrudService.Infrastructure.Tests/Waitlist/EmailNotificationObserverTests.cs`
- [X] T023 [US3] test(red): Fallo de auditoría (repo lanza excepción) no propaga: si `INotificationDeliveryRepository.AddAsync` falla, el observer captura la excepción y loguea sin propagar en `crud_service/tests/CrudService.Infrastructure.Tests/Waitlist/EmailNotificationObserverTests.cs`
- [X] T024 [US3] test(red): TC-HU5-04 — El correo no se envía antes de que el registro pending esté persistido: verificar que `INotificationDeliveryRepository.AddAsync` se invoca ANTES de `IEmailSender.SendOpportunityNotificationAsync` (orden de llamadas) en `crud_service/tests/CrudService.Infrastructure.Tests/Waitlist/EmailNotificationObserverTests.cs`

### Implementación (GREEN)

- [X] T025 [US3] feat(green): Ajustar `EmailNotificationObserver` si algún test de aislamiento falla — garantizar try-catch externo que cubra tanto el envío como la auditoría, y logging de fallos de repo en `crud_service/src/CrudService.Infrastructure/Services/EmailNotificationObserver.cs`

### Refactor

- [X] T026 refactor: Revisar cobertura completa de aislamiento — verificar que no existe ningún path en `EmailNotificationObserver` que pueda propagar una excepción al caller

**Checkpoint**: Los 7 tests del observer pasan (4 de Phase 3 + 3 de Phase 4). Aislamiento total verificado. `dotnet test CrudService.Infrastructure.Tests` verde.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Validación final y limpieza

- [X] T027 Ejecutar `dotnet test` completo del CRUD Service (Application.Tests + Infrastructure.Tests) y verificar 0 fallos
- [ ] T028 [P] Ejecutar quickstart.md contra infraestructura Docker y verificar flujo end-to-end (inscribir → liberar ticket → asignar → verificar log de correo + registro en notification_deliveries)
- [X] T029 [P] Verificar compilación limpia sin warnings: `dotnet build crud_service/CrudService.sln --warnaserror`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Hexagonal DDD (Phase 0)**: Sin dependencias — debe completarse antes de Phase 1 para no perpetuar violaciones arquitectónicas
- **Setup (Phase 1)**: Depende de Phase 0 (T-ARCH-03 compilación verde)
- **Foundational (Phase 2)**: Depende de Phase 1 (T002, T003, T004) — BLOQUEA todas las user stories
- **US1+US2 (Phase 3)**: Depende de Phase 2 completa (puertos, refactor, adaptadores base)
- **US3 (Phase 4)**: Depende de Phase 3 (el observer debe existir para probar aislamiento)
- **Polish (Phase 5)**: Depende de Phase 3 y Phase 4 completas

### User Story Dependencies

- **US1 + US2 (P1)**: Pueden iniciar tras Phase 2. Son inseparables (mismo observer hace envío + auditoría)
- **US3 (P2)**: Puede iniciar tras Phase 3 — verifica propiedades de aislamiento del observer ya implementado

### Within Each User Story (TDD Sequence)

1. **RED**: Escribir tests que fallan (T015-T018, T022-T024)
2. **GREEN**: Implementar mínimo código para pasar (T019-T020, T025)
3. **REFACTOR**: Limpiar sin romper tests (T021, T026)
4. **Verificar** que todos los tests pasan antes de avanzar

### Parallel Opportunities

```text
# Phase 0 — paralelos parciales:
T-ARCH-01 → T-ARCH-02 → T-ARCH-03  (secuencial: quitar atributos → mover mapeo → verificar)
T-ARCH-04 || T-ARCH-05              (paralelos: interfaces distintas en archivos distintos)
T-ARCH-04 + T-ARCH-05 → T-ARCH-06  (verificar tras mover ambas)

# Phase 1 — paralelos:
T002 || T003 || T004   (enum, record, entidad — archivos distintos sin dependencias)

# Phase 2 — paralelos tras T005-T009:
T010 || T011            (puertos nuevos — archivos distintos)

# Phase 3 — paralelos (tests RED):
T015 || T016 || T016b || T017 || T018 || T018c  (mismo archivo pero tests independientes)

# Phase 4 — paralelos (tests RED):
T022 || T023 || T024   (tests de aislamiento independientes)

# Phase 5 — paralelos:
T028 || T029            (quickstart vs. compilación)
```

---

## Implementation Strategy

### MVP First (US1 + US2)

1. Complete Phase 1: Setup (T001-T004)
2. Complete Phase 2: Foundational — refactor HU3 (T005-T014)
3. Complete Phase 3: US1 + US2 — observer con envío + auditoría (T015-T021)
4. **STOP and VALIDATE**: `dotnet test` verde, verificar log de correo + registro en BD
5. Deploy/demo si listo

### Incremental Delivery

1. Setup + Foundational → Refactor limpio, 7 tests HU3 siguen pasando
2. US1 + US2 → Observer funcional con auditoría → 4 tests nuevos pasan (MVP!)
3. US3 → Aislamiento verificado formalmente → 3 tests adicionales pasan
4. Polish → Validación completa end-to-end

---

## Notes

- [P] tasks = archivos diferentes, sin dependencias entre sí
- [US1], [US2], [US3] labels mapean a user stories de spec.md
- TDD estricto: cada test RED debe fallar antes de implementar GREEN
- Commit convention: `test(red): ...`, `feat(green): ...`, `refactor: ...`
- El observer `EmailNotificationObserver` implementa US1 (envío) + US2 (auditoría) + US3 (aislamiento) como una sola clase — el aislamiento es una propiedad transversal, no un componente separado
