# Implementation Plan: Autenticación de usuarios — HU1 + HU2

**Branch**: `001-user-auth` | **Date**: 2026-04-07 | **Spec**: [specs/001-user-auth/spec.md](spec.md)
**Input**: Feature specification from `/specs/001-user-auth/spec.md`

## Summary

Implementar el módulo de autenticación para la plataforma de venta de tickets:
- **HU1** — Registro de usuario comprador (`POST /api/auth/register`): validación de campos, unicidad de email, hash BCrypt y persistencia.
- **HU2** — Login/logout (`POST /api/auth/login`, `POST /api/auth/logout`): generación de JWT, bloqueo temporal tras 3 intentos fallidos, error genérico anti-enumeración.

Enfoque técnico: arquitectura Hexagonal (Ports & Adapters), TDD Red-Green-Refactor, .NET 8 / ASP.NET Core / EF Core / PostgreSQL.

---

## Technical Context

**Language/Version**: C# / .NET 8.0
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 8, BCrypt.Net-Next (work factor 12), System.IdentityModel.Tokens.Jwt
**Storage**: PostgreSQL 15
**Testing**: xUnit + NSubstitute (unit) | xUnit + Testcontainers PostgreSQL (integration)
**Target Platform**: Linux server (Docker, aspnet:8.0 image)
**Project Type**: Web service (REST API — microservicio)
**Performance Goals**: Respuestas de login/registro < 500 ms p95 en condiciones normales
**Constraints**: Sin refresh token; sin verificación de email; sin OAuth en esta iteración; rate limiting por IP fuera de scope (responsabilidad del API Gateway / reverse proxy)
**Scale/Scope**: MVP; se asume carga baja inicial (~1 k usuarios registrados)

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Regla constitucional | Estado | Evidencia |
|---|---------------------|--------|-----------|
| 1 | Arquitectura Hexagonal con 5 zonas separadas | ✅ PASS | La estructura de proyectos respeta Domain / Application / Infrastructure / Api |
| 2 | Domain sin dependencias externas | ✅ PASS | `AuthService.Domain` no referenciará EF Core ni BCrypt |
| 3 | Application solo depende de Domain | ✅ PASS | Use cases dependen solo de interfaces del dominio |
| 4 | TDD — ningún código sin test en rojo previo | ✅ PASS | Ciclo Red-Green-Refactor definido en plan de implementación |
| 5 | RN1: email único validado en `RegisterUserUseCase` | ✅ PASS | `IUserRepository.ExistsByEmail()` llamado antes de crear |
| 6 | RN2: reglas de contraseña validadas en `User.Create()` | ✅ PASS | Entidad lanza excepción si no cumple |
| 7 | RN3: passwords hasheadas con BCrypt, nunca texto plano | ✅ PASS | `IPasswordHashingService.Hash()` llamado en use case antes de persistir |
| 8 | RN4: solo usuarios registrados pueden hacer login | ✅ PASS | `LoginUserUseCase` lanza si el usuario no existe |
| 9 | RN5: bloqueo tras 3 intentos fallidos via `LockedState` + `AccountLockedException` | ✅ PASS | `LockedState.AttemptLogin()` lanza `AccountLockedException`; `LoginAttemptsRepository` persiste intentos |
| 10 | Secretos vía variables de entorno (`IOptions<T>`) | ✅ PASS | `JwtSettings`, `ConnectionStrings` inyectados desde env |
| 11 | Errores genéricos — sin revelar existencia del email | ✅ PASS | Mensaje único: `"Credenciales inválidas"` |
| 12 | Features fuera de alcance no implementadas | ✅ PASS | Sin refresh token, OAuth, verificación correo, etc. |

**POST-DISEÑO (re-evaluado tras Phase 1)**: sin violaciones detectadas.

---

## Project Structure

### Documentation (this feature)

```text
specs/001-user-auth/
├── plan.md           ← este archivo
├── research.md       ← Phase 0
├── data-model.md     ← Phase 1
├── quickstart.md     ← Phase 1
├── contracts/        ← Phase 1
│   ├── register.md
│   ├── login.md
│   └── logout.md
└── tasks.md          ← Phase 2 (/speckit.tasks)
```

### Source Code

```text
AuthService/
├── src/
│   ├── AuthService.Domain/
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   └── UserState.cs          # base abstracta (ActiveState, LockedState)
│   │   ├── Ports/
│   │   │   ├── Input/
│   │   │   │   ├── IRegisterUserUseCase.cs
│   │   │   │   └── ILoginUserUseCase.cs
│   │   │   └── Output/
│   │   │       ├── IUserRepository.cs
│   │   │       ├── ILoginAttemptsRepository.cs
│   │   │       └── IPasswordHashingService.cs
│   │   └── Exceptions/
│   │       ├── EmailAlreadyExistsException.cs
│   │       ├── InvalidPasswordException.cs
│   │       ├── InvalidCredentialsException.cs
│   │       └── AccountLockedException.cs
│   │
│   ├── AuthService.Application/
│   │   ├── UseCases/
│   │   │   ├── RegisterUserUseCase.cs
│   │   │   └── LoginUserUseCase.cs
│   │   └── DTOs/
│   │       ├── RegisterUserRequest.cs
│   │       ├── LoginRequest.cs
│   │       └── LoginResponse.cs
│   │
│   ├── AuthService.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Repositories/
│   │   │   │   ├── UserRepository.cs
│   │   │   │   └── LoginAttemptsRepository.cs
│   │   │   └── Migrations/
│   │   └── Services/
│   │       ├── BcryptPasswordService.cs
│   │       └── JwtTokenService.cs
│   │
│   └── AuthService.Api/
│       ├── Controllers/
│       │   ├── RegisterController.cs
│       │   └── LoginController.cs
│       ├── Settings/
│       │   ├── JwtSettings.cs
│       │   └── LockoutSettings.cs
│       ├── Program.cs
│       └── appsettings.json
│
└── tests/
    ├── AuthService.Application.Tests/
    │   ├── RegisterUserUseCaseTests.cs
    │   └── LoginUserUseCaseTests.cs
    └── AuthService.Infrastructure.Tests/
        ├── UserRepositoryTests.cs
        └── LoginAttemptsRepositoryTests.cs
```

**Structure Decision**: Opción multi-proyecto (4 proyectos) impuesta por la constitución (Hexagonal). Sin frontend — este es un microservicio REST puro.

---

## Complexity Tracking

> Sin violaciones constitucionales que justificar.
