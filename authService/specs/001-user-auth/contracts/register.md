# Contrato: Registro de usuario

**Endpoint**: `POST /api/auth/register`
**Caso de uso**: `RegisterUserUseCase`

---

## Request

```http
POST /api/auth/register
Content-Type: application/json
```

```json
{
  "firstName": "Ana",
  "lastName": "Perez",
  "email": "ana.perez@example.com",
  "password": "Secr3t@Pass",
  "confirmPassword": "Secr3t@Pass"
}
```

### Campos

| Campo | Tipo | Requerido | Validación |
|-------|------|-----------|-----------|
| `firstName` | `string` | ✅ | No vacío, max 100 |
| `lastName` | `string` | ✅ | No vacío, max 100 |
| `email` | `string` | ✅ | Formato email válido, max 255 |
| `password` | `string` | ✅ | ≥8 chars, ≥1 mayúscula, ≥1 carácter especial |
| `confirmPassword` | `string` | ✅ | Igual a `password` |

---

## Responses

### 201 Created — Registro exitoso

```json
{
  "message": "Registro exitoso. Serás redirigido al login.",
  "redirect": "/login"
}
```

### 400 Bad Request — Error de validación

```json
{
  "message": "Datos inválidos",
  "errors": {
    "email": "Formato de correo inválido",
    "password": "La contraseña no cumple los requisitos mínimos",
    "confirmPassword": "Las contraseñas no coinciden"
  }
}
```

### 409 Conflict — Email ya registrado

```json
{
  "message": "Este correo ya está en uso. ¿Deseas iniciar sesión?"
}
```

---

## Flujo interno

```
RegisterController
  └─► RegisterUserUseCase
        ├─ IUserRepository.ExistsByEmail(email)
        │    └─ true → lanza EmailAlreadyExistsException → 409
        ├─ User.Create(firstName, lastName, email.ToLower(), passwordHash)
        │    └─ falla invariante → InvalidPasswordException → 400
        ├─ IPasswordHashingService.Hash(password)
        └─ IUserRepository.Save(user) → 201
```

---

## Notas de seguridad

- `confirmPassword` no se persiste ni se envía al use case.
- `email` se normaliza a minúsculas antes de persistir.
- Solo el hash BCrypt (work factor ≥ 12) se almacena.
