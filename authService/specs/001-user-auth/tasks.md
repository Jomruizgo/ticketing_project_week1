# Tasks: Autenticación de usuarios — HU1 + HU2

**Input**: Design documents from `/specs/001-user-auth/`
**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/ ✅ | quickstart.md ✅
**Branch**: `001-user-auth`
**Generated**: 2026-04-07

## Format: `[ID] [P?] [Story?] Description — ruta`

- **[P]**: Paralelizable (archivos distintos, sin dependencia de tarea incompleta)
- **[HU1]**: Historia HU1 — Registro de usuario comprador
- **[HU2]**: Historia HU2 — Login y logout
- Sin label de story → fase Setup, Foundational o Polish

---

## Phase 1: Setup

**Purpose**: Inicializar la solución .NET multi-proyecto con la estructura hexagonal definida en `plan.md`.

- [x] T001 Crear solución .NET y 4 proyectos: `dotnet new sln -n AuthService && dotnet new classlib/webapi` — `src/AuthService.Domain`, `src/AuthService.Application`, `src/AuthService.Infrastructure`, `src/AuthService.Api`
- [x] T002 Agregar referencias entre proyectos (Domain ← Application ← Infrastructure ← Api) y NuGet: BCrypt.Net-Next, EF Core, Npgsql, JWT, xUnit, NSubstitute, Testcontainers — `AuthService.sln`
- [x] T003 [P] Crear `docker-compose.yml` con servicio `postgres:15` en raíz del repositorio
- [x] T004 [P] Crear `src/AuthService.Api/appsettings.json` y `appsettings.Development.json.example` con secciones `ConnectionStrings`, `JwtSettings`, `LockoutSettings`

**Checkpoint**: `dotnet build` pasa sin errores; `docker compose up -d postgres` levanta PostgreSQL.

---

## Phase 2: Foundational

**Purpose**: Dominio puro + puertos + infraestructura base que bloquea todas las HUs.

**⚠️ CRÍTICO**: Ninguna HU puede implementarse hasta completar esta fase.

- [x] T005 Crear excepciones de dominio: `EmailAlreadyExistsException`, `InvalidPasswordException`, `InvalidCredentialsException`, `AccountLockedException` — `src/AuthService.Domain/Exceptions/`
- [x] T006 [P] Definir puerto de entrada `IRegisterUserUseCase` con método `ExecuteAsync(RegisterUserRequest)` — `src/AuthService.Domain/Ports/Input/IRegisterUserUseCase.cs`
- [x] T007 [P] Definir puerto de entrada `ILoginUserUseCase` con método `ExecuteAsync(LoginRequest)` — `src/AuthService.Domain/Ports/Input/ILoginUserUseCase.cs`
- [x] T008 [P] Definir puerto de salida `IUserRepository` (`ExistsByEmail`, `FindByEmail`, `Save`, `Update`) — `src/AuthService.Domain/Ports/Output/IUserRepository.cs`
- [x] T009 [P] Definir puerto de salida `ILoginAttemptsRepository` (`RecordAttempt`, `CountRecentFailures`) — `src/AuthService.Domain/Ports/Output/ILoginAttemptsRepository.cs`
- [x] T010 [P] Definir puerto de salida `IPasswordHashingService` (`Hash`, `Verify`) — `src/AuthService.Domain/Ports/Output/IPasswordHashingService.cs`
- [x] T011 Implementar entidad `User` con Factory Method `User.Create()`, `UserState` abstracta, `ActiveState`, `LockedState`, métodos `RecordFailedAttempt()`, `LockUntil()`, `ResetFailedAttempts()`, `IsLocked()` — `src/AuthService.Domain/Entities/User.cs`, `src/AuthService.Domain/Entities/UserState.cs`
- [x] T012 Crear `AppDbContext` con `DbSet<User>` y `DbSet<LoginAttempt>`, configurar constraints (email UNIQUE, campo `locked_until` nullable) via Fluent API — `src/AuthService.Infrastructure/Persistence/AppDbContext.cs`
- [x] T013 Generar migración inicial EF Core: `dotnet ef migrations add InitialCreate` y verificar SQL generado coincide con `data-model.md` — `src/AuthService.Infrastructure/Persistence/Migrations/`
- [x] T014 [P] Configurar `Program.cs`: registrar DbContext, use cases, repositories, password service, JWT, configurar Swagger — `src/AuthService.Api/Program.cs`
- [x] T015 [P] Crear `JwtSettings.cs` y `LockoutSettings.cs` como records de configuración inyectados vía `IOptions<T>` — `src/AuthService.Api/Settings/`

**Checkpoint**: `dotnet build` pasa; `dotnet ef database update` aplica migración; `dotnet run` arranca la API con Swagger en `/swagger`.

---

## Phase 3: HU1 — Registro de usuario comprador (Priority: P1) 🎯 MVP

**Goal**: Endpoint `POST /api/auth/register` funcional con validación completa, hash BCrypt y persistencia.

**Independent Test**: `POST /api/auth/register` con payload válido → 201 y usuario en BD con `passwordHash`. Email duplicado → 409 mensaje exacto.

### Tests — HU1 (Red antes de implementar)

- [x] T016 [P] [HU1] Escribir `RegisterUserUseCaseTests`: `Success_WhenValid`, `Fails_WhenEmailExists` (espera 409), `Fails_WhenPasswordInvalid` (espera 400) — `tests/AuthService.Application.Tests/RegisterUserUseCaseTests.cs`

### Implementación — HU1

- [x] T017 [P] [HU1] Crear DTOs `RegisterUserRequest` (firstName, lastName, email, password, confirmPassword con anotaciones de validación) y `RegisterUserResponse` — `src/AuthService.Application/DTOs/RegisterUserRequest.cs`
- [x] T018 [HU1] Implementar `RegisterUserUseCase`: verificar email único (`IUserRepository.ExistsByEmail`), hashear con `IPasswordHashingService.Hash()`, crear `User.Create()`, persistir (`IUserRepository.Save`) — `src/AuthService.Application/UseCases/RegisterUserUseCase.cs`
- [x] T019 [HU1] Implementar `UserRepository` con EF Core: `ExistsByEmail` (normaliza a minúsculas), `FindByEmail`, `Save`, `Update` — `src/AuthService.Infrastructure/Persistence/Repositories/UserRepository.cs`
- [x] T020 [HU1] Implementar `RegisterController` (`POST /api/auth/register`): validar ModelState → llamar use case → mapear excepciones a 400/409 → devolver 201 — `src/AuthService.Api/Controllers/RegisterController.cs`
- [x] T021 [HU1] Test de integración E2E: `POST /api/auth/register` flujo exitoso + email duplicado contra Testcontainers PostgreSQL — `tests/AuthService.Infrastructure.Tests/RegisterFlowTests.cs`

**Checkpoint**: `dotnet test tests/AuthService.Application.Tests` pasa (T016 verde). `POST /api/auth/register` responde 201, 400 y 409 correctamente según curl en `quickstart.md`.

---

## Phase 4: HU2 — Inicio y cierre de sesión (Priority: P1)

**Goal**: Endpoints `POST /api/auth/login` y `POST /api/auth/logout` funcionales con JWT, bloqueo tras 3 fallos y error genérico.

**Independent Test**: `POST /api/auth/login` con credenciales válidas → 200 con `token` JWT y `expiresIn`. 3 intentos fallidos → 401 genérico y cuenta bloqueada 15 minutos.

### Tests — HU2 (Red antes de implementar)

- [x] T022 [P] [HU2] Escribir `LoginUserUseCaseTests`: `Success_ReturnsToken`, `Fails_WhenUserNotFound` (401 genérico), `Fails_WhenPasswordInvalid` (401 + registra intento), `LocksAccount_AfterThreeFailures`, `ReturnsGenericError_WhenLocked` — `tests/AuthService.Application.Tests/LoginUserUseCaseTests.cs`

### Implementación — HU2

- [x] T023 [P] [HU2] Crear DTOs `LoginRequest` (email, password) y `LoginResponse` (token, expiresIn) — `src/AuthService.Application/DTOs/LoginRequest.cs`, `src/AuthService.Application/DTOs/LoginResponse.cs`
- [x] T024 [P] [HU2] Implementar `BcryptPasswordService` (`Hash` con work factor 12, `Verify`) — `src/AuthService.Infrastructure/Services/BcryptPasswordService.cs`
- [x] T025 [P] [HU2] Implementar `JwtTokenService` (`GenerateToken(User)` → signed JWT con claims `sub`, `email`, `iat`, `exp`) — `src/AuthService.Infrastructure/Services/JwtTokenService.cs`
- [x] T026 [HU2] Implementar `LoginAttemptsRepository` (`RecordAttempt`, `CountRecentFailures`) — `src/AuthService.Infrastructure/Persistence/Repositories/LoginAttemptsRepository.cs`
- [x] T027 [HU2] Implementar `LoginUserUseCase`: buscar usuario (hash dummy si no existe), delegar a `user.State.AttemptLogin()`, registrar intento, generar JWT en éxito, lanzar `InvalidCredentialsException` en fallo — `src/AuthService.Application/UseCases/LoginUserUseCase.cs`
- [x] T028 [HU2] Implementar `LoginController` (`POST /api/auth/login` → 200/401; `POST /api/auth/logout` → 200) — `src/AuthService.Api/Controllers/LoginController.cs`
- [x] T029 [HU2] Test de integración E2E: login exitoso, login fallido x3 + verificar bloqueo 15 min, logout → 200 — `tests/AuthService.Infrastructure.Tests/LoginFlowTests.cs`

**Checkpoint**: `dotnet test` pasa completo (T022 verde). Flujo completo de `quickstart.md` ejecuta sin errores: registro → login → token obtenido → logout.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Hardening, containerización y validación de escenarios de quickstart.

- [x] T030 [P] Crear `Dockerfile` multi-stage (build con `sdk:8.0`, runtime con `aspnet:8.0`, usuario no-root) — `Dockerfile`
- [x] T031 [P] Agregar middleware global de manejo de excepciones que mapea `EmailAlreadyExistsException` → 409, `InvalidPasswordException` → 400, `InvalidCredentialsException` → 401 con cuerpos JSON estandarizados — `src/AuthService.Api/Middleware/ExceptionHandlingMiddleware.cs`
- [x] T032 Ejecutar escenarios del `quickstart.md` completos (registro, login, bloqueo, logout) y corregir cualquier discrepancia — validación manual

**Checkpoint final**: `docker build -t authservice:local .` exitoso. `dotnet test` 100% verde. Todos los escenarios de `quickstart.md` pasan.

---

## Dependencies & Execution Order

### Dependencia entre fases

```
Phase 1 (Setup)
    └─► Phase 2 (Foundational) ← BLOQUEA todo
            ├─► Phase 3 (HU1 — MVP)
            └─► Phase 4 (HU2) ─── puede empezar en paralelo con HU1 si hay 2 personas
                        └─► Phase 5 (Polish)
```

### Dependencia entre HUs

- **HU1 (P1)**: Puede iniciarse en cuanto Phase 2 esté completa. Sin dependencia de HU2.
- **HU2 (P1)**: Puede iniciarse en cuanto Phase 2 esté completa. Usa `UserRepository` creado en HU1 (T019) — si se trabaja en paralelo, mock de `IUserRepository` en tests unitarios.

### Dentro de cada HU

1. Tests (Red) primero — deben fallar antes de implementar
2. DTOs + Ports antes que use cases
3. Use cases antes que controllers
4. Repositories antes que use cases que los consumen
5. Tests verdes verificados antes de mover al siguiente ítem

### Oportunidades de paralelismo

```bash
# Phase 2 — Fundacional (tasks paralelas en equipo de 2+)
T006 & T007 & T008 & T009 & T010  # todos los ports en paralelo
T014 & T015                         # configuración en paralelo (distintos archivos)

# Phase 3 — HU1 (en un solo sprint)
T016 & T017  # test + DTO en paralelo (distintos archivos, ambos en rojo)

# Phase 4 — HU2
T022 & T023 & T024 & T025  # tests + DTOs + services en paralelo
```

---

## Implementation Strategy

**MVP**: Completar Phase 1 + Phase 2 + Phase 3 (HU1) → endpoint de registro funcional, testeable e independiente.

**Incremento 2**: Phase 4 (HU2) → login/logout con JWT y bloqueo de cuenta.

**Entrega final**: Phase 5 → Docker + middleware de errores + validación quickstart.

---

## Resumen

| Fase | Tareas | Paralelizables |
|------|--------|---------------|
| Phase 1 — Setup | T001–T004 | T003, T004 |
| Phase 2 — Foundational | T005–T015 | T006–T010, T014–T015 |
| Phase 3 — HU1 (P1) MVP | T016–T021 | T016, T017 |
| Phase 4 — HU2 (P1) | T022–T029 | T022–T025 |
| Phase 5 — Polish | T030–T032 | T030, T031 |
| **Total** | **32 tareas** | **17 paralelizables** |

**MVP scope**: Phases 1–3 (T001–T021) — 21 tareas, entregable independiente con HU1 completa.
