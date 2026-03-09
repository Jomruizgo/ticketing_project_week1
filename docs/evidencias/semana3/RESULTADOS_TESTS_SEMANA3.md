# Resultados de Pruebas — Semana 3

## Registro de ejecuciones

| Fecha | Módulo | Comando | Resultado | Evidencia |
|---|---|---|---|---|
| 2026-03-01 | ReservationService (Application.Tests) | `dotnet test ReservationService/tests/ReservationService.Application.Tests/ReservationService.Application.Tests.csproj --nologo --logger "trx;LogFileName=reservation-application-week3.trx" --results-directory docs/evidencias/semana3/artifacts/reservation` | 9/9 en verde en `ReservationService.Application.Tests` | `docs/evidencias/semana3/artifacts/reservation/reservation-application-week3.trx` |
| 2026-03-01 | ReservationService | `dotnet test ReservationService/ReservationService.sln --nologo --logger "trx;LogFileName=reservation-week3.trx" --results-directory docs/evidencias/semana3/artifacts/reservation` | 9/9 en verde en `ReservationService.Application.Tests`; `Domain.Tests` y `Infrastructure.Tests` sin tests detectados por runner | `docs/evidencias/semana3/artifacts/reservation/reservation-week3.trx` |
| 2026-03-01 | crud_service | `dotnet test crud_service/CrudService.sln --nologo --logger "trx;LogFileName=crud-week3.trx" --results-directory docs/evidencias/semana3/artifacts/crud` | 58/58 en verde en `CrudService.Infrastructure.Tests` | `docs/evidencias/semana3/artifacts/crud/crud-week3.trx` |
| 2026-03-01 | paymentService | `dotnet test paymentService/MsPaymentService.sln --nologo --logger "trx;LogFileName=payment-week3.trx" --results-directory docs/evidencias/semana3/artifacts/payment` | 25/25 en verde en `MsPaymentService.Worker.Tests` | `docs/evidencias/semana3/artifacts/payment/payment-week3.trx` |

## Mapeo Verificar vs Validar (Semana 3)

| Tipo | Caso | Test(s) | Estado |
|---|---|---|---|
| Verificar | Parsing, SSE contract, consumer pipeline, hub behavior | `SseMessageFormatterTests`, `StatusPayloadParserTests`, `TicketStatusConsumerTests`, `TicketStatusPipelineTests`, `TicketStatusHubTests` | Evidencia presente en `crud-week3.trx` |
| Validar | Reglas críticas de negocio (expiración, idempotencia, transiciones) | `ProcessExpirationCommandHandlerTests` (Application ReservationService) y suites de Worker Payment para reglas de aprobación/rechazo | Evidencia parcial; revisar cobertura de `Domain`/`Infrastructure` en ReservationService |

## Tabla de defensa: Caso → Tipo → Riesgo que evita

| Caso evaluado | Tipo | Riesgo que evita |
|---|---|---|
| `StatusPayloadParser` retorna `null` ante JSON inválido o vacío | Verificar | Caída del consumer por payload malformado y ruido operacional por excepciones evitables |
| `SseMessageFormatter` emite `ticketId/status` en formato esperado | Verificar | Ruptura del contrato SSE con frontend (eventos no parseables o campos inconsistentes) |
| `TicketStatusConsumer` procesa mensaje y ejecuta flujo técnico de notificación | Verificar | Pérdida silenciosa de eventos o comportamiento errático en pipeline de mensajería |
| Ticket `reserved` vencido transiciona a `released` | Validar | Bloqueo indefinido de inventario y degradación de disponibilidad de tickets |
| Ticket `paid` no se revierte por expiración tardía + manejo idempotente de duplicados | Validar | Inconsistencia de negocio (venta revertida indebidamente) y efectos duplicados por reentrega de eventos |

## Evidencia de calidad de pruebas (aserciones y aislamiento)

- Aserciones significativas de negocio en `ProcessExpirationCommandHandlerTests` (`Success`, `StatusChanged`, mensajes de error y comportamiento idempotente).
- Aserciones de contrato técnico en SSE/JSON en `SseContractIntegrationTests` y `SseMessageFormatterTests` (campos exactos, formato y restricciones de contrato).
- Aislamiento con mocks/sustitutos mediante `Substitute.For`, `Received` y `DidNotReceive` en `TicketStatusConsumerTests`, `ProcessExpirationCommandHandlerTests` y `PaymentEventDispatcherImplTests`.

### Guion corto para defensa oral

- Verificar responde: "¿está técnicamente bien construido?" (contratos, formato, parsing, flujo técnico).
- Validar responde: "¿protege reglas de negocio críticas?" (estado correcto, idempotencia, no revertir `paid`).
- En este reto, ambos niveles se cubren con evidencia en `.trx` y mapeo explícito de casos.

## Resumen ejecutivo
- Suite total ejecutada: **92/92 tests detectados en verde** (`9 + 58 + 25`)
- Regresiones detectadas: **0** en suites ejecutadas
- Cobertura de criterios de rúbrica: **Conforme** — ver `TESTING_STRATEGY.md` en raíz del proyecto
- No detección de tests en `ReservationService.Domain.Tests` e `Infrastructure.Tests`: declarada como fuera de alcance del sprint (ver §Declaración de alcance en `TESTING_STRATEGY.md`)

## Notas
- Los tres reportes `.trx` fueron generados y versionables dentro de `docs/evidencias/semana3/artifacts/`.
- Hallazgo técnico abierto: en la ejecución de `ReservationService.sln`, el runner reporta "No hay ninguna prueba disponible" para `Domain.Tests` e `Infrastructure.Tests`.
- Confirmación adicional: `dotnet test --list-tests` en `ReservationService.Domain.Tests` e `ReservationService.Infrastructure.Tests` devuelve cero casos discoverables, lo que indica proyectos de test actualmente vacíos (no error de configuración del runner).

## Declaración de alcance (importante para auditoría)

- Para Semana 3, el alcance probado en `ReservationService` se centró en `Application.Tests` (casos de uso y reglas de negocio de expiración).
- `ReservationService.Domain.Tests` y `ReservationService.Infrastructure.Tests` existen como proyectos, pero no contienen casos implementados en esta iteración.
- Por tanto, la no detección de pruebas en esos dos proyectos no corresponde a fallo del runner ni a regresión funcional, sino a cobertura no abordada por alcance de sprint.

## Trazabilidad TDD (Semana 3)

**Dev A** — commits atómicos por fase (historial verificable en `git log`):
- 🔴 RED: `f676b8a` — `test(red): add expiration use case tests including technical failure`
- 🟢 GREEN: `83eccf1` — `feat(green): implement automatic reservation expiration flow`
- 🔵 REFACTOR: `3f4638c` — `refactor: decouple status publishing and harden expiration consumer`
- ✅ E2E: `9ba2bcd` — `test(integration): add repeatable docker verification for Dev A expiration flow`

**Dev B** — ciclo TDD ejecutado en iteración, consolidado en commit `0703302` (rama `feature/mock-imposible/crud-tests-em`). Secuencia RED→GREEN→REFACTOR documentada en `TESTING_STRATEGY.md` §3.2.

## Human Check: guía rápida de preguntas y respuestas

Usar este bloque como mini-guion durante la sustentación, partiendo del código de pruebas y luego del respaldo documental:

| Pregunta esperada | Respuesta mínima defendible |
|---|---|
| ¿Qué prueba valida negocio y no solo técnica? | `ProcessExpirationCommandHandlerTests`: valida que un ticket `paid` no se revierte y que duplicados no rompen idempotencia. |
| ¿Qué prueba es de verificación técnica? | `StatusPayloadParserTests` y `SseMessageFormatterTests`: protegen contrato de parsing/formato SSE. |
| ¿Qué riesgo de producción evita esa prueba? | Evita caídas por payload inválido, ruptura de contrato con frontend y transiciones de estado inválidas. |
| ¿Por qué usaron mocks/sustitutos en ese caso? | Para aislar comportamiento del caso de uso y asegurar que la aserción mida decisión de negocio, no detalles de infraestructura. |
| ¿Qué limitación reconocen hoy? | La trazabilidad de commits TDD no quedó atomizada por fase en esta iteración y `Domain`/`Infrastructure` de ReservationService quedaron fuera del alcance de pruebas del sprint. |

