# Data Model: Autenticación de usuarios — HU1 + HU2

**Feature**: `001-user-auth` | **Date**: 2026-04-07

---

## Entidades del dominio

### User

Entidad raíz del dominio de autenticación. Estado inmutable desde fuera; se modifica solo mediante métodos de dominio.

| Campo | Tipo | Constraints | Notas |
|-------|------|-------------|-------|
| `Id` | `Guid` | PK, NOT NULL | generado en `User.Create()` — `Guid.NewGuid()` |
| `FirstName` | `string` | NOT NULL, max 100 | requerido en registro |
| `LastName` | `string` | NOT NULL, max 100 | requerido en registro |
| `Email` | `string` | NOT NULL, UNIQUE, max 255 | normalizado a minúsculas antes de persistir |
| `PasswordHash` | `string` | NOT NULL | hash BCrypt, work factor ≥ 12 |
| `LockedUntil` | `DateTime?` | nullable | `null` = activo; fecha futura = bloqueado |
| `FailedLoginAttempts` | `int` | NOT NULL, default 0 | se resetea al hacer login exitoso |
| `CreatedAt` | `DateTime` | NOT NULL | UTC, asignado en `User.Create()` |

**Invariantes (validadas en `User.Create()`):**
- `Email` debe tener formato `^[\w.-]+@[\w.-]+\.[A-Za-z]{2,}$`.
- `Password` (antes de hashear) debe cumplir `^(?=.*[A-Z])(?=.*[^A-Za-z0-9]).{8,}$`.

**Métodos de dominio:**
- `User.Create(firstName, lastName, email, passwordHash)` → `User` (Factory Method — valida invariantes)
- `RecordFailedAttempt()` → incrementa `FailedLoginAttempts`; si alcanza 3, llama `LockUntil(DateTime.UtcNow.AddMinutes(15))`
- `LockUntil(DateTime until)` → establece `LockedUntil`
- `ResetFailedAttempts()` → `FailedLoginAttempts = 0; LockedUntil = null`
- `IsLocked()` → `bool`: `LockedUntil != null && LockedUntil > DateTime.UtcNow`

**Patrón State:**
- `UserState` (abstracta): `AttemptLogin(password, hashedPassword, passwordService)`
- `ActiveState`: valida credenciales, registra intento fallido
- `LockedState`: siempre lanza `InvalidCredentialsException` sin verificar password

```
User
 ├── State: UserState (calculado — no persistido directamente)
 │    ├── ActiveState   (cuando IsLocked() == false)
 │    └── LockedState   (cuando IsLocked() == true)
 ```

---

### LoginAttempt (persistida en Infrastructure)

Registro de auditoría de intentos de login. No es una entidad de dominio pura — vive en Infrastructure como proyección de eventos.

| Campo | Tipo | Constraints | Notas |
|-------|------|-------------|-------|
| `Id` | `Guid` | PK | |
| `UserId` | `Guid?` | FK nullable → `User.Id` | null si el email no existe |
| `EmailAttempted` | `string` | NOT NULL, max 255 | email tal como fue enviado |
| `IpAddress` | `string?` | max 45 | IPv4/IPv6 |
| `AttemptedAt` | `DateTime` | NOT NULL | UTC |
| `Successful` | `bool` | NOT NULL | true = login exitoso |

---

## Diagrama de relaciones

```
User (1) ────────────────── (0..N) LoginAttempt
         [Id]                       [UserId nullable FK]
```

---

## Tablas PostgreSQL (esquema inferido)

```sql
-- users
CREATE TABLE users (
    id               UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    first_name       VARCHAR(100) NOT NULL,
    last_name        VARCHAR(100) NOT NULL,
    email            VARCHAR(255) NOT NULL UNIQUE,
    password_hash    TEXT         NOT NULL,
    locked_until     TIMESTAMPTZ,
    failed_login_attempts INT     NOT NULL DEFAULT 0,
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- login_attempts
CREATE TABLE login_attempts (
    id               UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id          UUID         REFERENCES users(id) ON DELETE SET NULL,
    email_attempted  VARCHAR(255) NOT NULL,
    ip_address       VARCHAR(45),
    attempted_at     TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    successful       BOOLEAN      NOT NULL
);
```

---

## Reglas de validación (resumen)

| Regla | Entidad/Use Case | Comportamiento en fallo |
|-------|-----------------|------------------------|
| Email formato válido | `User.Create()` | lanza `InvalidEmailException` |
| Email único en BD | `RegisterUserUseCase` | lanza `EmailAlreadyExistsException` → 409 |
| Password: ≥8 chars, 1 mayúscula, 1 especial | `User.Create()` | lanza `InvalidPasswordException` → 400 |
| Password = ConfirmPassword | `RegisterController` (DTO validation) | devuelve 400 |
| Credenciales correctas | `LoginUserUseCase` | lanza `InvalidCredentialsException` → 401 |
| Bloqueo tras 3 fallos | `User.RecordFailedAttempt()` | `LockedState` → 401 genérico |

---

## Transiciones de estado de User

```
         crear
[nuevo] ──────────────────→ [Activo]
                                │
                     3 fallos   │  login exitoso
                   consecutivos │◄───────────────────┐
                                ↓                    │
                          [Bloqueado]                │
                                │                    │
                   15 min       │                    │
                   expiran      ↓                    │
                         [Activo]  ──────────────────┘
```
