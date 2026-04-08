# Feature Specification: Autenticación de usuarios (HU1, HU2)

**Feature Branch**: `001-user-auth`
**Created**: 2026-04-07
**Status**: Draft
**Input**: User description: "Autenticación de usuarios para plataforma de venta de tickets. HU1 — Registro de nuevo usuario comprador. HU2 — Inicio y cierre de sesión. (ver .github/prompts)"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Registro de usuario comprador (Priority: P1)

Como nuevo visitante, quiero crear una cuenta para tener una identidad única en el sistema y acceder a mis futuros tickets.

**Why this priority**: Permite la conversión de visitantes a compradores y es requisito previo para venta y gestión de tickets.

**Independent Test**: Llamar `POST /api/auth/register` con payload válido; verificar respuesta 201 y registro en BD con password hasheada.

**Acceptance Scenarios**:
1. **Given** formulario con todos los campos válidos, **When** el usuario envía `POST /api/auth/register`, **Then** responde 201 y se crea el usuario (hash en DB).
2. **Given** contraseña que no cumple reglas, **When** envía el formulario, **Then** responde 400 y no se crea el usuario.
3. **Given** email ya registrado, **When** intenta registrar, **Then** responde 409 con mensaje: "Este correo ya está en uso. ¿Deseas iniciar sesión?".

---

### User Story 2 - Inicio y cierre de sesión (Priority: P1)

Como usuario registrado, quiero iniciar sesión y cerrarla para acceder a mi historial de tickets y proteger mi cuenta.

**Why this priority**: Acceso seguro a funcionalidades protegidas; imprescindible para la experiencia del usuario.

**Independent Test**: Llamar `POST /api/auth/login` con credenciales válidas; verificar 200 con token JWT. Simular 3 fallos y verificar bloqueo por 15 minutos.

**Acceptance Scenarios**:
1. **Given** credenciales correctas, **When** `POST /api/auth/login`, **Then** devuelve 200 con `{ token, expiresIn }`.
2. **Given** credenciales incorrectas, **When** `POST /api/auth/login`, **Then** devuelve 401 con `{ "message": "Credenciales inválidas" }` y registra intento fallido.
3. **Given** 3 intentos fallidos consecutivos, **When** se intenta login, **Then** la cuenta queda bloqueada temporalmente y cualquier intento devuelve 401 genérico.
4. **Given** un usuario autenticado, **When** `POST /api/auth/logout`, **Then** devuelve 200 `{ "message": "Logout exitoso" }` (cliente elimina token).

### Edge Cases

- Intentos de registro con email con mayúsculas/espacios: normalizar email al guardarlo.
- Intentos masivos de login desde la misma IP: aplicar rate limiting y delays.
- Usuario inexistente + intento de login: responder 401 genérico sin revelar existencia.
- El desbloqueo de cuenta es automático tras expirar `LockoutSettings.LockoutMinutes` (por defecto 15 minutos). El sistema evalúa `lockedUntil` en tiempo real en cada intento — no se requiere un proceso de limpieza periódico.
- Token JWT expirado presentado en cualquier endpoint protegido retorna 401. El cliente debe realizar un nuevo login para obtener un token válido.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Permitir registro de usuario con `firstName`, `lastName`, `email`, `password`, `confirmPassword`.
- **FR-002**: Validar formato de `email` y unicidad en BD; si existe, devolver 409 con mensaje indicado.
- **FR-003**: Validar `password` con reglas: mínimo 8 caracteres, al menos 1 mayúscula, 1 carácter especial.
- **FR-004**: Almacenar solo hash de la contraseña; usar `IPasswordHashingService.Hash()` (BCrypt, work factor mínimo = 12). Este es un requisito de seguridad — no un detalle de implementación.
- **FR-005**: Permitir login con `email` y `password`, generando JWT firmado con algoritmo HS256. El token debe incluir los claims obligatorios: `sub` (GUID del usuario), `email`, `iat` (issued at), `exp` (expiration). La respuesta incluye `expiresIn` en segundos (valor por defecto: 3600, equivalente a 60 minutos; configurable vía `JwtSettings.ExpirationMinutes`).
- **FR-006**: Registrar intentos fallidos y bloquear cuenta tras 3 fallos consecutivos por 15 minutos (configurable vía `LockoutSettings.LockoutMinutes`). "3 consecutivos" significa 3 fallos sin ningún login exitoso intermedio. Un login exitoso reinicia el contador de intentos fallidos a 0.
- **FR-007**: Logout devuelve 200 y cliente elimina token; servidor puede opcionalmente mantener blacklist.
- **FR-008**: Mensajes de error de autenticación deben ser genéricos para evitar user enumeration.

### Security Constraints

- **SEC-001**: El JWT debe firmarse con algoritmo HS256 (`HmacSha256`).
- **SEC-002**: El claim `sub` del JWT debe ser el GUID del usuario (`userId`), no un valor numérico secuencial (previene IDOR si otros microservicios consumen el token directamente).
- **SEC-003**: Los claims obligatorios del token son: `sub` (GUID del usuario), `email`, `iat`, `exp`.

### Key Entities

- **User**: `id`, `firstName`, `lastName`, `email` (único, normalizado), `passwordHash`, `lockedUntil` (nullable), `createdAt`.
- **LoginAttempt**: `id`, `userId` (nullable), `emailAttempted`, `ip`, `timestamp`, `successful`.
- **TokenBlacklist** (opcional): `token`, `expiresAt`.
- **AccountLockedException** *(excepción de dominio)*: lanzada por `LockedState.AttemptLogin()` cuando la cuenta está bloqueada temporalmente (tras 3 intentos fallidos consecutivos). Se mapea a HTTP 401 con mensaje genérico `"Credenciales inválidas"` para no revelar el motivo real.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 95% de registros válidos completados con respuesta 201 y usuario creado (test automatizado).
- **SC-002**: 100% de intentos de registro con email duplicado devuelven 409 con el mensaje exacto.
- **SC-003** *(monitoreo post-despliegue — fuera del buildable scope)*: Respuestas de login válidas < 1 s p95 en producción. Justificación: BCrypt introduce ~300 ms por diseño de seguridad; un benchmark automatizado depende del hardware del entorno y no forma parte de los criterios de aceptación de las HUs ni del scope del sprint actual. Se monitoreará en producción, no en CI.
- **SC-004**: Tras 3 intentos fallidos, la cuenta queda bloqueada y cualquier intento durante 15 minutos devuelve 401 genérico.

## Assumptions

- No se implementa verificación por correo ni recuperación de contraseña en esta iteración.
- No se implementan refresh tokens; JWT tiene expiración corta configurable.
- La persistencia será PostgreSQL disponible en entorno de integración.
- **Rate limiting por IP**: fuera de alcance del microservicio AuthService. Justificación: RN5 cubre la protección contra fuerza bruta a nivel de cuenta; el rate limiting por IP es responsabilidad de la capa de infraestructura (API Gateway / reverse proxy), fuera del scope del microservicio en esta iteración.

---

## Tests recomendados

- Unitarios (Application layer):
  - `RegisterUserUseCase_CreateUser_Success_WhenValid()` — verifica hash y persistencia.
  - `RegisterUserUseCase_Fails_WhenEmailExists()` — espera excepción que se mapea a 409.
  - `LoginUserUseCase_IncrementsAttempts_AndLocksOnThirdFailure()` — simula 3 fallos y verifica bloqueo.

- Integración (Infrastructure + Api):
  - E2E: migraciones + `POST /api/auth/register` + `POST /api/auth/login` flujo exitoso.
  - Escenario de bloqueo: 3 fallos y bloqueo 15 minutos.

## Deliverables

- Archivo `spec.md` (este documento).
- DTOs de request/response para register/login/logout.
- Tests unitarios e integración descritos.

---

Si quieres, implemento ahora los DTOs y los tests unitarios para `RegisterUserUseCase` y `LoginUserUseCase`.
