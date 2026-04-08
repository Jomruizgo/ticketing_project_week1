# Checklist: Autenticación de usuarios — HU1 + HU2

**Propósito**: Unit tests para los requisitos escritos. Valida que el documento de especificación (`spec.md`) sea completo, claro, consistente y listo para implementar — no verifica si el código funciona.
**Creado**: 2026-04-07
**Feature**: `001-user-auth`
**Tipo**: Estándar (PR Review) — cobertura completa
**Actor**: Revisor de PR antes de merge

---

## Completitud de Requisitos

*¿Están documentados todos los requisitos necesarios?*

- [x] CHK001 — ¿Los campos `firstName` y `lastName` en FR-001 incluyen restricciones máximas de longitud (ej. 100 chars)? → Aceptado: límite estándar web (255 chars) aplicado por validación de modelo. [Completeness, Spec §FR-001]
- [x] CHK002 — ¿La estructura exacta del response body para `POST /api/auth/register` (201) está especificada en el spec, o sólo existe en los contratos? → Cubierto en `specs/001-user-auth/contracts/register.md`. [Completeness, Gap]
- [x] CHK003 — ¿Los nombres exactos de los claims del JWT (`sub`, `email`, `iat`, `exp`) están documentados como requisito en FR-005, o sólo en research.md? → Resuelto: SEC-001/SEC-002/SEC-003 agregados en spec.md §Security Constraints. [Completeness, Spec §FR-005]
- [x] CHK004 — ¿El campo `expiresIn` incluido en la respuesta de login está definido como requisito en FR-005, con unidad (segundos/minutos) especificada? → Resuelto: expiresIn en segundos, default 3600, configurable vía JwtSettings.ExpirationMinutes. [Completeness, Spec §FR-005]
- [x] CHK005 — ¿El esquema de autenticación Bearer requerido en los headers HTTP está documentado como requisito (no sólo en los contratos)? → Cubierto en constitution §XII y contracts/logout.md. [Completeness, Gap]
- [x] CHK006 — ¿Los mensajes de error de validación de contraseña para cada regla incumplida (longitud, mayúscula, carácter especial) están especificados individualmente? → Aceptado: mensajes individuales responsabilidad del modelo de validación ASP.NET; spec define la regla (RN2), no el string exacto. [Completeness, Spec §FR-003]
- [x] CHK007 — ¿El comportamiento de reset del contador de intentos fallidos tras un login exitoso está documentado como requisito? → Resuelto: agregado en FR-006 spec.md. [Completeness, Gap]
- [x] CHK008 — ¿El campo `failedLoginAttempts` del usuario está incluido explícitamente en Key Entities, o se asume implícito del patrón State? → Cubierto: tabla `login_attempts` documentada en constitution §XIII y data-model.md. [Completeness, Spec - Key Entities]

---

## Claridad de Requisitos

*¿Son los requisitos específicos y sin ambigüedad?*

- [x] CHK009 — ¿"Configurable" en FR-006 ("bloqueo configurable") especifica quién configura el valor, dónde (appsettings) y cuál es el valor por defecto? → Resuelto en FR-006: LockoutSettings.MaxFailedAttempts=3, LockoutSettings.LockoutMinutes=15 vía appsettings.json. [Clarity, Spec §FR-006]
- [x] CHK010 — ¿"Normalizar email" en los Edge Cases define la estrategia exacta concreta (lowercase + trim)? → Aceptado: lowercase + trim aplicado en capa de aplicación antes de persistir. Decisión de implementación, no requisito funcional. [Clarity, Spec - Edge Cases]
- [x] CHK011 — ¿"3 fallos consecutivos" en FR-006 define explícitamente si un login exitoso reinicia el contador o si el bloqueo es absoluto? → Resuelto: login exitoso reinicia contador a 0, documentado en FR-006 spec.md. [Clarity, Ambiguity, Spec §FR-006]
- [x] CHK012 — ¿FR-007 ("cliente elimina token") especifica si eso es suficiente o si el servidor tiene requisito adicional de invalidación activa (blacklist)? → Resuelto: logout stateless, TokenBlacklist fuera de alcance v1. Documentado en spec y requirements.md. [Clarity, Ambiguity, Spec §FR-007]
- [x] CHK013 — ¿FR-008 (mensajes genéricos anti-enumeración) documenta el string exacto del mensaje de error para 401, o se deja a criterio del implementador? → Cubierto: string exacto `"Credenciales inválidas"` definido en constitution §VIII y contracts/login.md. [Clarity, Spec §FR-008]
- [x] CHK014 — ¿El tiempo de expiración del JWT está cuantificado como requisito (ej. 60 minutos por defecto), o sólo mencionado como "configurable"? → Resuelto: default 3600 segundos (60 min), configurable vía JwtSettings.ExpirationMinutes, agregado en FR-005. [Clarity, Spec §FR-005]

---

## Consistencia de Requisitos

*¿Los requisitos se alinean entre sí sin conflictos?*

- [x] CHK015 — ¿FR-007 (logout stateless) es consistente con la entidad `TokenBlacklist` listada como "opcional" en Key Entities? → Resuelto: TokenBlacklist eliminada de Key Entities. Logout es stateless v1. [Consistency, Spec §FR-007]
- [x] CHK016 — ¿La exclusión de SC-003 (latencia de login < 1 s) como criterio de aceptación es consistente con los acceptance scenarios de HU2 que no mencionan rendimiento? → Resuelto: SC-003 reclasificado como métrica de monitoreo post-despliegue. [Consistency, Spec §SC-003]
- [x] CHK017 — ¿El mensaje exacto `"Credenciales inválidas"` en Scenario 2 de HU2 es consistente con el requisito de respuesta genérica en FR-008? → Consistente: mismo string en spec, constitution y contracts. [Consistency, Spec §FR-008]
- [x] CHK018 — ¿Los Edge Cases (rate limiting por IP) que se mencionan en el spec son consistentes con la Assumption que los excluye del scope del microservicio? → Consistente: rate limiting por IP documentado como fuera de alcance en Assumptions y requirements.md. [Consistency, Spec - Assumptions]

---

## Calidad de Criterios de Aceptación

*¿Los criterios de éxito son medibles y verificables?*

- [x] CHK019 — ¿SC-001 ("95% de registros válidos → 201") es verificable con un test automatizado unitario/integración, o requiere carga con múltiples peticiones? → Cubierto por RegisterFlowTests (4 tests) + RegisterUserUseCaseTests (unitarios). Sin necesidad de test de carga. [Measurability, Spec §SC-001]
- [x] CHK020 — ¿SC-002 incluye el string exacto del mensaje 409 que debe ser afirmado programáticamente en el test? → Cubierto: Register_DuplicateEmail_Returns409 verifica HttpStatusCode.Conflict. String del mensaje en contracts/register.md. [Measurability, Spec §SC-002]
- [x] CHK021 — ¿SC-004 (bloqueo tras 3 intentos) especifica si el test automatizado debe simular el paso del tiempo (15 min) o si es suficiente verificar el estado inmediato post-bloqueo? → Resuelto: test verifica estado inmediato post-bloqueo (Login_ThreeFailedAttempts_AccountLocked_Returns401). El desbloqueo por tiempo se evalúa en tiempo real. [Acceptance Criteria, Spec §SC-004]
- [x] CHK022 — ¿Los 4 acceptance scenarios de HU2 tienen trazabilidad 1-a-1 con al menos un success criterion (SC-001 a SC-004)? → Verificado: login exitoso → SC-001, credenciales inválidas → FR-008, bloqueo → SC-004, logout → FR-007. [Traceability, Spec - User Story 2]

---

## Cobertura de Escenarios

*¿Todos los flujos relevantes están definidos?*

- [x] CHK023 — ¿Está documentado el flujo de recuperación cuando expira el bloqueo de 15 minutos — el usuario se desbloquea automáticamente o requiere intervención? → Resuelto: desbloqueo automático por evaluación de lockedUntil en tiempo real, agregado en Edge Cases de spec.md. [Coverage, Gap]
- [x] CHK024 — ¿El escenario de logout sin header `Authorization` (usuario anónimo) tiene un código de respuesta definido? → Cubierto: Logout_WithoutToken_Returns401 en LoginFlowTests. Respuesta 401. [Coverage, Gap]
- [x] CHK025 — ¿El escenario de uso de un token JWT expirado en un recurso protegido (`POST /api/auth/logout`) tiene respuesta documentada? → Resuelto: token expirado → 401, usuario debe hacer login nuevamente. Agregado en Edge Cases spec.md. [Coverage, Gap]
- [x] CHK026 — ¿El flujo de registro con `confirmPassword` distinto de `password` está documentado explícitamente como escenario 400 en los acceptance scenarios de HU1? → Cubierto: Register_PasswordMismatch_Returns400 en RegisterFlowTests. [Coverage, Spec - User Story 1]

---

## Cobertura de Edge Cases

*¿Están definidas las condiciones límite?*

- [x] CHK027 — ¿El comportamiento cuando `lockedUntil` está en el pasado (bloqueo expirado) está definido — se evalúa en tiempo real o se requiere un proceso de limpieza? → Resuelto: evaluación en tiempo real en cada intento de login. Sin proceso de limpieza. Documentado en Edge Cases spec.md. [Edge Case, Gap]
- [x] CHK028 — ¿El comportamiento ante valores nulos o strings vacíos en `email` o `password` en el request de login está especificado? → Cubierto: validación de modelo ASP.NET retorna 400 ante campos vacíos/nulos. Cubierto por FR-002 y validación de campos obligatorios. [Edge Case, Gap]
- [x] CHK029 — ¿El edge case de dos requests simultáneos que completan el tercer intento fallido (race condition en bloqueo) está mencionado como asunción o requisito de consistencia? → Aceptado como limitación v1: race condition documentada como deuda técnica conocida. Mitigación futura con transacciones DB. [Edge Case, Gap]

---

## Requisitos No Funcionales — Seguridad

*¿Los atributos de calidad de seguridad están especificados?*

- [x] CHK030 — ¿El algoritmo de firma JWT (HS256) está documentado como requisito de seguridad en la especificación, o sólo como detalle de implementación en research.md? → Resuelto: SEC-001 agregado en spec.md §Security Constraints. [Security, Research §3]
- [x] CHK031 — ¿El work factor de BCrypt (12) está expresado como requisito mínimo de seguridad en el spec (FR-004), o sólo como decisión de diseño en research.md? → Resuelto: "work factor mínimo = 12" agregado en FR-004 spec.md. [Security, Spec §FR-004]
- [x] CHK032 — ¿Existe un requisito mínimo de expiración del JWT para limitar la ventana de exposición ante robo de token? → Cubierto por CHK004/CHK014: expiresIn default 3600s (60 min) definido como requisito en FR-005. [Security, Gap]
- [x] CHK033 — ¿El tipo del campo `sub` en el JWT (GUID) está documentado como requisito para prevenir IDOR si otros microservicios lo consumen directamente? → Resuelto: SEC-002 (sub=GUID, anti-IDOR) agregado en spec.md §Security Constraints. [Security, Spec §FR-005]

---

## Dependencias y Suposiciones

*¿Están documentadas y validadas?*

- [x] CHK034 — ¿La suposición "no verificación por email en esta iteración" está marcada como deuda técnica conocida o gap a cubrir en un sprint futuro? → Cubierto: verificación de email documentada como fuera de alcance v1 en spec.md §Assumptions y requirements.md. [Assumption, Spec - Assumptions]
- [x] CHK035 — ¿La dependencia de PostgreSQL para tests de integración con Testcontainers está documentada en quickstart.md como requisito de entorno de CI? → Cubierto: GitHub Actions `ubuntu-latest` incluye Docker nativo. Testcontainers levanta PostgreSQL automáticamente. Sin configuración adicional requerida. [Dependency, Gap]
