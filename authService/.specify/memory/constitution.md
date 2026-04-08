# AuthService Constitution

## I. Arquitectura Hexagonal (NO NEGOCIABLE)

El AuthService implementa exclusivamente **arquitectura Hexagonal (Ports and Adapters)**. Esta decisión es irrevocable porque todos los demás microservicios del ecosistema (ReservationService, PaymentService) ya la implementan y la rúbrica lo exige explícitamente.

**Cinco zonas obligatorias — ninguna puede fusionarse con otra:**

| Zona | Capa | Componentes |
|------|------|-------------|
| ① Adaptadores de Entrada | Infrastructure Driving | `RegisterController`, `LoginController` |
| ② Puertos de Entrada | Domain Input Ports | `IRegisterUserUseCase`, `ILoginUserUseCase` |
| ③ Casos de Uso | Application | `RegisterUserUseCase`, `LoginUserUseCase` |
| ④ Puertos de Salida | Domain Output Ports | `IUserRepository`, `ILoginAttemptsRepository`, `IPasswordHashingService` |
| ⑤ Adaptadores de Salida | Infrastructure Driven | `UserRepository`, `LoginAttemptsRepository`, `BcryptPasswordService` |

**Regla de dependencia:** Las flechas apuntan siempre hacia el dominio. El dominio no importa nada de `Microsoft.EntityFrameworkCore`, `BCrypt.Net` ni ningún framework externo. Cualquier código en `AuthService.Domain` o `AuthService.Application` que referencie una librería de infraestructura es una violación de esta constitución.

**Estructura de proyectos obligatoria:**
```
AuthService/
├── src/
│   ├── AuthService.Domain/        ← Sin dependencias externas
│   ├── AuthService.Application/   ← Solo depende de Domain
│   ├── AuthService.Infrastructure/← Depende de Domain + libs externas
│   └── AuthService.Api/           ← Composition Root + Controllers
└── tests/
    ├── AuthService.Application.Tests/    ← Mocks de puertos
    └── AuthService.Infrastructure.Tests/ ← DB real / Testcontainers
```

---

## II. TDD — Red-Green-Refactor (NO NEGOCIABLE)

El ciclo TDD es la única forma válida de escribir código de producción en este servicio:

1. **Red:** escribir el test que falla (el código aún no existe)
2. **Green:** escribir el mínimo código que hace pasar el test
3. **Refactor:** mejorar sin romper los tests

**Nunca** se escribe código de producción sin un test en rojo previo. Esto aplica a entidades, casos de uso y adaptadores.

**Pirámide de tests:**
- **Unitarios** (`Application.Tests`): casos de uso con mocks de `IUserRepository`, `ILoginAttemptsRepository` e `IPasswordHashingService`. Rápidos, sin I/O.
- **Integración** (`Infrastructure.Tests`): repositorios EF Core contra PostgreSQL real o Testcontainers. Verifican queries, migraciones y constraints.
- **No** se escriben tests que emulen comportamiento de librerías externas (ej. no testear que BCrypt hashea correctamente — eso es responsabilidad de BCrypt.Net).

---

## III. Principios SOLID

Cada clase, interfaz y método debe poder justificarse contra estos principios:

- **SRP:** cada clase tiene una única razón para cambiar. `RegisterUserUseCase` no valida HTTP; `RegisterController` no tiene lógica de negocio.
- **OCP:** agregar una nueva regla de negocio no modifica casos de uso existentes. Se extiende, no se modifica.
- **LSP:** cualquier implementación de un puerto (`IUserRepository`) es intercambiable sin que el caso de uso se rompa.
- **ISP:** interfaces pequeñas y cohesivas. `IUserRepository` no mezcla operaciones de usuarios con operaciones de intentos de login.
- **DIP:** los casos de uso dependen de interfaces definidas en el dominio. Nunca de `UserRepository` concreto, nunca de `BcryptPasswordService` concreto.

**Señal de alerta:** si un constructor de un caso de uso recibe un parámetro de tipo concreto (no interfaz), es una violación de DIP.

---

## IV. Programación Orientada a Objetos

Los cuatro pilares de OOP deben ser evidentes en el código:

- **Encapsulación:** `User` no expone setters públicos. El estado se modifica únicamente a través de métodos de dominio (`LockAccount()`, `RecordFailedAttempt()`, `ResetFailedAttempts()`).
- **Abstracción:** los puertos (`IUserRepository`, etc.) son abstracciones; los casos de uso trabajan con ellas sin conocer la implementación.
- **Herencia:** el patrón State usa `UserState` como clase base abstracta con `ActiveState` y `LockedState` como subclases concretas.
- **Polimorfismo:** `LoginUserUseCase` llama `user.State.AttemptLogin(...)` sin saber si el estado es `Active` o `Locked`; el comportamiento varía según el estado actual.

---

## V. Patrones de Diseño GoF Aplicados

Solo los patrones con justificación técnica concreta son válidos. Los siguientes están pre-aprobados:

| Patrón | Ubicación | Justificación |
|--------|-----------|---------------|
| **Factory Method** | `User.Create(email, hashedPassword)` | Garantiza que ningún `User` inválido exista en memoria; centraliza invariantes del dominio |
| **Adapter** | `BcryptPasswordService`, `UserRepository`, `LoginAttemptsRepository` | Traduce APIs externas incompatibles a interfaces del dominio |
| **Facade** | `RegisterUserUseCase`, `LoginUserUseCase` | Simplifica la orquestación compleja de subsistemas para los controladores |
| **State** | `User` + `ActiveState` / `LockedState` | Elimina condicionales dispersos para RN5; cada estado encapsula su comportamiento |
| **Strategy** ⚠️ | `IPasswordHashingService` | **Solo para testabilidad**: permite inyectar mock en tests unitarios evitando los ~300ms de BCrypt. No se usa para intercambiar algoritmos en producción |

**Advertencia sobre Strategy:** cambiar el algoritmo de hasheo en producción invalida todos los hashes en DB. Un cambio de algoritmo requiere migración de datos, no un swap de implementación.

---

## VI. Reglas de Negocio — Invariantes del Dominio

Las siguientes reglas **nunca** pueden violarse; si un caso de uso las ignora, el código está incorrecto:

| ID | Regla | Dónde se valida |
|----|-------|-----------------|
| **RN1** | El email debe ser único en la base de datos | `RegisterUserUseCase` — consulta `IUserRepository.ExistsByEmail()` antes de crear |
| **RN2** | Contraseña: mínimo 8 caracteres, al menos 1 mayúscula y 1 carácter especial | `User.Create()` en el dominio |
| **RN3** | Las contraseñas se almacenan hasheadas con bcrypt. Nunca en texto plano | `RegisterUserUseCase` — llama `IPasswordHashingService.Hash()` antes de persistir |
| **RN4** | Solo usuarios registrados pueden iniciar sesión | `LoginUserUseCase` — lanza excepción si el usuario no existe |
| **RN5** | Bloqueo temporal tras 3 intentos fallidos consecutivos | `LockedState` en la entidad `User`; `LoginAttemptsRepository` persiste los intentos |

---

## VII. Alcance — Qué está dentro y qué no

**Dentro del alcance (HU1 + HU2):**
- Registro de usuario con nombre, apellido, email, contraseña
- Inicio de sesión con generación de JWT
- Cierre de sesión (invalidación de token en cliente)
- Bloqueo de cuenta tras 3 intentos fallidos

**Fuera del alcance — no implementar, no diseñar, no mencionar en código:**
- Verificación de correo electrónico
- Recuperación de contraseña
- Autenticación con terceros (OAuth, Google, GitHub)
- Refresh token
- Eliminación de cuenta
- Gestión dinámica de roles
- Edición de perfil

Cualquier feature fuera del alcance que aparezca en una especificación debe ser rechazada con justificación explícita.

---

## VIII. Seguridad

- **Secretos:** ninguna credencial (JWT secret, connection string, RabbitMQ password) se hardcodea. Se inyectan via variables de entorno y `IOptions<T>`.
- **JWT:** los tokens tienen tiempo de expiración definido (`JwtSettings.ExpirationMinutes`). Sin refresh token en esta iteración.
- **CORS:** política permisiva (`AllowAnyOrigin`) solo en desarrollo. En producción se restringe al dominio del frontend.
- **Contraseñas:** BCrypt con work factor ≥ 12. Nunca SHA-256, MD5 ni texto plano.
- **Errores:** los mensajes de error de autenticación no revelan si el email existe o no (prevención de user enumeration). Respuesta genérica: `"Credenciales inválidas"`.
- **Dockerfile:** multi-stage build, imagen final basada en `aspnet:8.0`, sin correr como `root`.

---

## IX. Stack Técnico

| Componente | Tecnología |
|------------|------------|
| Runtime | .NET 8.0 (C#) |
| API | ASP.NET Core Web API |
| ORM | Entity Framework Core |
| Base de datos | PostgreSQL 15 |
| Hash de contraseñas | BCrypt.Net-Next (work factor 12) |
| Autenticación | JWT — `System.IdentityModel.Tokens.Jwt` |
| Tests unitarios | xUnit + NSubstitute (o Moq) |
| Tests integración | xUnit + Testcontainers (PostgreSQL) |
| Contenerización | Docker multi-stage |
| CI/CD | GitHub Actions |

---

## X. Workflow de Desarrollo

- **Rama activa:** `feature/login` → PR a `develop` → merge a `main`
- **Commits:** granulares, un commit por ciclo Red-Green o por componente completado
- **CI obligatorio:** todo PR debe pasar Build + Unit Tests + Integration Tests + Docker Build. Un pipeline rojo bloquea el merge.
- **Convención de nombres:**
  - Interfaces: prefijo `I` — `IUserRepository`
  - Casos de uso: sufijo `UseCase` — `RegisterUserUseCase`
  - Controladores: sufijo `Controller` — `RegisterController`
  - Adaptadores de salida: nombre descriptivo sin sufijo especial — `BcryptPasswordService`, `UserRepository`

---

## XI. Definition of Done

Una tarea está completa cuando se cumplen **todos** los siguientes criterios sin excepción:

- El test en rojo existe y falla antes de escribir el código de producción (TDD)
- Todos los tests unitarios pasan (`AuthService.Application.Tests`)
- Todos los tests de integración pasan (`AuthService.Infrastructure.Tests`)
- El pipeline de CI está en verde (Build + Unit + Integration + Docker)
- Ninguna clase en `AuthService.Domain` o `AuthService.Application` importa librerías externas (`EntityFrameworkCore`, `BCrypt.Net`, `System.IdentityModel`, etc.)
- El código puede justificarse contra los cinco principios SOLID de esta constitución
- Los constructores de casos de uso solo reciben interfaces, nunca clases concretas

---

## XII. Contratos de API

Los siguientes endpoints son los únicos que expone AuthService. Cualquier endpoint adicional requiere aprobación explícita.

| Endpoint | Método | Body / Header | Response éxito | Response error |
|----------|--------|---------------|----------------|----------------|
| `/api/auth/register` | POST | `{ firstName, lastName, email, password, confirmPassword }` | `201 { message: "Usuario registrado exitosamente" }` | `400` campos inválidos / `409` email duplicado |
| `/api/auth/login` | POST | `{ email, password }` | `200 { token, expiresAt }` | `401` credenciales inválidas / `423` cuenta bloqueada |
| `/api/auth/logout` | POST | `Authorization: Bearer <token>` | `200 { message: "Sesión cerrada" }` | `401` token inválido o ausente |

**Formato de error estándar — siempre este shape, sin variaciones:**
```json
{ "error": "Descripción genérica del problema" }
```

**Regla de user enumeration:** los errores de login nunca revelan si el email existe o no. La respuesta para "email no encontrado" y "contraseña incorrecta" es idéntica: `"Credenciales inválidas"`.

**Códigos HTTP:**
- `400` — validación de formato o campos obligatorios faltantes
- `401` — credenciales incorrectas o token ausente/inválido
- `409` — conflicto de unicidad (email ya registrado)
- `423` — cuenta bloqueada por intentos fallidos (RN5)

---

## XIII. Convenciones de Base de Datos

- **Tablas:** snake_case en plural — `users`, `login_attempts`
- **IDs:** `GUID` (`Guid` en C#), nunca autoincremental. Generado en el dominio, no en la base de datos
- **Columnas:** snake_case — `first_name`, `password_hash`, `failed_attempts`, `locked_until`
- **Migraciones:** EF Core Code First. Una migración por cambio de esquema. Nunca modificar una migración ya aplicada — siempre crear una nueva
- **Schema inicial obligatorio:**

```
users
├── id              UUID        PK
├── first_name      VARCHAR     NOT NULL
├── last_name       VARCHAR     NOT NULL
├── email           VARCHAR     UNIQUE NOT NULL
├── password_hash   VARCHAR     NOT NULL
├── created_at      TIMESTAMP   NOT NULL
└── state           VARCHAR     NOT NULL  -- 'active' | 'locked'

login_attempts
├── id              UUID        PK
├── user_id         UUID        FK → users.id
├── attempted_at    TIMESTAMP   NOT NULL
└── success         BOOLEAN     NOT NULL
```

---

## Governance

Esta constitución tiene precedencia sobre cualquier otra práctica, convención o preferencia personal. Toda decisión de diseño que contradiga estos principios debe documentarse con justificación explícita y aprobarse antes de implementarse.

Las siguientes situaciones requieren revisión de la constitución antes de proceder:
- Agregar una dependencia nueva en `AuthService.Domain` o `AuthService.Application`
- Implementar una feature marcada como "fuera del alcance"
- Cambiar el algoritmo de hasheo de contraseñas
- Modificar la estructura de capas del proyecto

**Version**: 1.1.0 | **Ratified**: 2026-04-07 | **Last Amended**: 2026-04-07
