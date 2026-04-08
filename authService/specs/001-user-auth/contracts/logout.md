# Contrato: Cierre de sesión

**Endpoint**: `POST /api/auth/logout`

---

## Request

```http
POST /api/auth/logout
Authorization: Bearer <token>
```

Sin body. El token se envía en el header `Authorization`.

---

## Responses

### 200 OK — Logout exitoso

```json
{
  "message": "Logout exitoso"
}
```

### 401 Unauthorized — Token ausente o inválido

```json
{
  "message": "No autorizado"
}
```

---

## Comportamiento

El logout en esta iteración es **stateless en el servidor**: la responsabilidad de eliminar el token recae en el cliente (eliminar del almacenamiento local). El endpoint confirma la operación con 200.

**Opción futura (fuera de alcance v1)**: añadir `TokenBlacklistRepository` para invalidar tokens activos hasta su expiración natural.

```
LoginController.Logout()
  └─► Validar JWT (middleware)
        └─ válido → devolver 200 { "message": "Logout exitoso" }
        └─ inválido/ausente → 401
```

---

## Notas

- El cliente **debe** eliminar el token del almacenamiento local al recibir 200.
- Sin token en `Authorization` → 401.
- Acceso a vistas protegidas tras logout queda bloqueado porque el cliente no tiene token.
