# Research: Autenticación de usuarios — HU1 + HU2

**Feature**: `001-user-auth` | **Date**: 2026-04-07

Todas las incógnitas técnicas estaban resueltas por la constitución o por decisiones de diseño conocidas. No quedó ningún ítem como NEEDS CLARIFICATION.

---

## 1. Arquitectura — Hexagonal (Ports & Adapters)

**Decisión**: Arquitectura Hexagonal con 5 zonas: Domain, Application, Infrastructure, Api, Tests.

**Rationale**: Exigida por la rúbrica y ya implementada en los demás microservicios del ecosistema (ReservationService, PaymentService). Garantiza testabilidad unitaria sin I/O y separación absoluta de frameworks.

**Alternativas consideradas**: Arquitectura en capas tradicional (MVC) — rechazada porque acopla la lógica de negocio al ORM y dificulta el TDD sin base de datos.

---

## 2. Hash de contraseñas

**Decisión**: BCrypt.Net-Next, work factor 12.

**Rationale**: Algoritmo de hashing adaptativo resistente a ataques de fuerza bruta; el work factor 12 equilibra seguridad y latencia (~300 ms en hardware moderno). Cumple OWASP ASVS L2.

**Alternativas consideradas**:
- SHA-256 / MD5: rechazados — funciones hash rápidas no aptas para contraseñas.
- Argon2 (via Konscious.Security.Cryptography): candidato válido, pero BCrypt.Net-Next ya está como dependencia declarada en la constitución.

**Nota Strategy pattern**: `IPasswordHashingService` se usa exclusivamente para permitir mock en tests unitarios (evitar ~300 ms por test). No implica swap de algoritmo en producción. Un cambio de algoritmo requiere migración de hashes — no es un swap simple.

---

## 3. Autenticación — JWT

**Decisión**: JWT generado con `System.IdentityModel.Tokens.Jwt`; sin refresh token.

**Rationale**: Estándar de la industria para APIs REST stateless. El alcance explícitamente excluye refresh token en esta iteración (ver constitución § VII).

**Claims mínimos del token**:
- `sub` = userId (GUID)
- `email`
- `iat` (issued at)
- `exp` = `iat + JwtSettings.ExpirationMinutes`

**Alternativas consideradas**: ASP.NET Core Identity — rechazado porque impone un modelo de datos rígido y acopla capas; contradice la arquitectura hexagonal.

---

## 4. Bloqueo de cuenta tras intentos fallidos

**Decisión**: Patrón State en la entidad `User` (`ActiveState` / `LockedState`) + `LoginAttemptsRepository` para persistencia. Duración: 15 minutos configurable.

**Rationale**: El patrón State elimina condicionales dispersos `if (user.IsLocked)` en el use case; el comportamiento varía por polimorfismo. Cumple RN5 de la constitución.

**Alternativas consideradas**:
- Contador simple en `User`: más sencillo pero no encapsula el comportamiento del estado bloqueado.
- Redis con TTL: más escalable para multi-instancia, pero introduce dependencia innecesaria en un MVP single-instance.

---

## 5. Seguridad — anti-enumeración de usuarios

**Decisión**: Mensaje de error único `"Credenciales inválidas"` para login fallido, independientemente de si el email existe o no.

**Rationale**: Previene ataques de user enumeration (OWASP A07:2021). La constitución lo exige explícitamente (§ VIII).

**Implementación**: En `LoginUserUseCase`, si el usuario no existe se ejecuta un hash dummy (`IPasswordHashingService.Hash("dummy")`) para igualar el tiempo de respuesta antes de devolver el error.

---

## 6. ORM y migraciones

**Decisión**: Entity Framework Core 8 con migraciones code-first.

**Rationale**: Integración nativa con .NET, soporte para PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL`, migraciones versionadas en código.

**Alternativas consideradas**: Dapper (micro-ORM) — más performante, pero requiere SQL manual y gestión manual de esquema.

---

## 7. Tests de integración

**Decisión**: Testcontainers para PostgreSQL en `AuthService.Infrastructure.Tests`.

**Rationale**: Permite ejecutar tests contra una BD real sin depender de un servidor externo en CI. Cada test suite levanta un contenedor efímero.

**Alternativas consideradas**: SQLite en memoria — descartado porque comportamiento difiere de PostgreSQL (constraints, tipos, migraciones).

---

## 8. Normalización de email

**Decisión**: Normalizar email a minúsculas antes de almacenar y buscar.

**Rationale**: Evita duplicados por diferencia de case (`Ana@email.com` vs `ana@email.com`). Implementado como Value Object o en `User.Create()`.

**Alternativas consideradas**: Collation case-insensitive en PostgreSQL — funciona, pero delega la lógica de negocio a la base de datos.
