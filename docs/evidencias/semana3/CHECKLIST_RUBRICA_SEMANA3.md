# Checklist de Rúbrica — Semana 3

> Estado: **Conforme**

## 1) Integridad y eficacia de la suite
- [x] 100% de pruebas en verde (en suites detectadas por el runner)
- [x] Sin regresiones en pruebas heredadas (en suites ejecutadas)
- [x] Pruebas con aserciones significativas (sin "teatro de calidad")
- [x] Mocks aislando comportamiento (no detalles de implementación)

**Evidencia:**
- [x] Link/archivo de resultado automatizado
- [x] Captura de ejecución en verde (evidencia automatizada vía archivos `.trx` en `docs/evidencias/semana3/artifacts/` — sustituyen capturas manuales; ver `TESTING_STRATEGY.md` §7)
- [x] Aserciones de negocio/técnicas en `ReservationService/tests/ReservationService.Application.Tests/ProcessExpirationCommandHandlerTests.cs`
- [x] Contratos y formato SSE validados en `crud_service/tests/CrudService.Infrastructure.Tests/Integration/SseContractIntegrationTests.cs` y `crud_service/tests/CrudService.Infrastructure.Tests/Messaging/SseMessageFormatterTests.cs`
- [x] Mocks con aislamiento vía `Substitute.For`, `Received` y `DidNotReceive` en `crud_service/tests/CrudService.Infrastructure.Tests/Messaging/TicketStatusConsumerTests.cs` y `paymentService/MsPaymentService.Worker.Tests/PaymentEventDispatcherImplTests.cs`

## 2) Ciclo TDD IA-Native
- [x] Evidencia RED (prueba falla primero)
- [x] Evidencia GREEN (mínimo código para pasar)
- [x] Evidencia REFACTOR (sin romper pruebas)
- [x] Historial de commits muestra la secuencia — Dev A: `f676b8a`(RED) → `83eccf1`(GREEN) → `3f4638c`(REFACTOR); Dev B: ciclo documentado en `TESTING_STRATEGY.md` §3.2
- [x] TDD realizado en la iteración (Dev A atómico por fase; Dev B ciclo en iteración consolidado + documentado)

**Evidencia:**
- [x] Commits referenciados — `f676b8a`, `83eccf1`, `3f4638c`, `9ba2bcd` (Dev A); `0703302` (Dev B)
- [x] Nota explicativa por iteración — `TESTING_STRATEGY.md` §3

## 3) Reportes automatizados
- [x] Reporte generado automáticamente (ej. `.trx`)
- [x] Comando reproducible de generación
- [x] Ubicación/versionado o artifact accesible

**Evidencia:**
- [x] Ruta(s) de reporte
- [x] Fecha de ejecución

## 4) Verificar vs Validar
- [x] Casos técnicos (verificar) identificados
- [x] Casos de negocio (validar) identificados
- [x] Trazabilidad caso -> test

**Evidencia:**
- [x] Tabla de mapeo en `RESULTADOS_TESTS_SEMANA3.md`

## 5) Human Check (comprensión)
- [x] El equipo puede explicar tests complejos (mocks/asserts/intención)
- [x] Se documentan decisiones de diseño de pruebas
- [x] La explicación parte del código de pruebas y luego se respalda en documentación

**Evidencia:**
- [x] Referencias a tests concretos en código
- [x] Q&A técnico o notas de defensa

---

## Veredicto interno
- Estado actual: **Conforme (Nivel Experto)**
- Riesgos abiertos: _No detección de tests en `ReservationService.Domain.Tests` y `ReservationService.Infrastructure.Tests` — declarado y explicado en `TESTING_STRATEGY.md` §Declaración de alcance._
- Evidencia visual: sustituida por reportes `.trx` automáticos en `docs/evidencias/semana3/artifacts/` (Dev A, Dev B, PaymentService) más `TESTING_STRATEGY.md`.
- Acción siguiente: ninguna pendiente para alcanzar nivel 5.0 experto.
