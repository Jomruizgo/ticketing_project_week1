# Contrato: Inicio de sesión

**Endpoint**: `POST /api/auth/login`
**Caso de uso**: `LoginUserUseCase`

---

## Request

```http
POST /api/auth/login
Content-Type: application/json
```

```json
{
  "email": "ana.perez@example.com",
  "password": "Secr3t@Pass"
}
```

### Campos

| Campo | Tipo | Requerido | Validación |
|-------|------|-----------|-----------|
| `email` | `string` | ✅ | Formato email válido |
| `password` | `string` | ✅ | No vacío |

---

## Responses

### 200 OK — Login exitoso

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600
}
```

**Claims del JWT:**

| Claim | Valor |
|-------|-------|
| `sub` | userId (GUID) |
| `email` | email del usuario |
| `iat` | timestamp actual (Unix) |
| `exp` | `iat + JwtSettings.ExpirationMinutes * 60` |

### 400 Bad Request — Campos inválidos

```json
{
  "message": "Datos inválidos",
  "errors": {
    "email": "Formato de correo inválido"
  }
}
```

### 401 Unauthorized — Credenciales inválidas o cuenta bloqueada

```json
{
  "message": "Credenciales inválidas"
}
```

> **Importante**: este mensaje es idéntico para credenciales incorrectas, usuario inexistente y cuenta bloqueada. No se revela cuál fue la causa específica (anti-enumeración).

---

## Flujo interno

```
LoginController
  └─► LoginUserUseCase
        ├─ IUserRepository.FindByEmail(email)
        │    └─ null → hash dummy + lanza InvalidCredentialsException → 401
        ├─ user.State.AttemptLogin(password, user.PasswordHash, IPasswordHashingService)
        │    ├─ LockedState → lanza InvalidCredentialsException → 401
        │    └─ ActiveState: IPasswordHashingService.Verify(password, hash)
        │         ├─ falla → user.RecordFailedAttempt()
        │         │           └─ FailedAttempts == 3 → user.LockUntil(+15min)
        │         │           lanza InvalidCredentialsException → 401
        │         └─ éxito → user.ResetFailedAttempts()
        └─ JwtTokenService.GenerateToken(user) → { token, expiresIn } → 200
```

---

## Notas de seguridad

- Si el usuario no existe, se ejecuta un hash dummy para equiparar el tiempo de respuesta y evitar timing attacks.
- El mensaje de error es siempre `"Credenciales inválidas"`, independientemente de la causa.
- Tras 3 intentos fallidos consecutivos la cuenta se bloquea 15 minutos (configurable en `AppSettings`).
- No se implementa refresh token en esta iteración.
