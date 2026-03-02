# Checklist de Rúbrica — Semana 3

> Estado: **En progreso**

## 1) Integridad y eficacia de la suite
- [x] 100% de pruebas en verde (en suites detectadas por el runner)
- [x] Sin regresiones en pruebas heredadas (en suites ejecutadas)
- [x] Pruebas con aserciones significativas (sin "teatro de calidad")
- [x] Mocks aislando comportamiento (no detalles de implementación)

**Evidencia:**
- [x] Link/archivo de resultado automatizado
- [ ] Captura de ejecución en verde
- [x] Aserciones de negocio/técnicas en `ReservationService/tests/ReservationService.Application.Tests/ProcessExpirationCommandHandlerTests.cs`
- [x] Contratos y formato SSE validados en `crud_service/tests/CrudService.Infrastructure.Tests/Integration/SseContractIntegrationTests.cs` y `crud_service/tests/CrudService.Infrastructure.Tests/Messaging/SseMessageFormatterTests.cs`
- [x] Mocks con aislamiento vía `Substitute.For`, `Received` y `DidNotReceive` en `crud_service/tests/CrudService.Infrastructure.Tests/Messaging/TicketStatusConsumerTests.cs` y `paymentService/MsPaymentService.Worker.Tests/PaymentEventDispatcherImplTests.cs`

## 2) Ciclo TDD IA-Native
- [x] Evidencia RED (prueba falla primero)
- [x] Evidencia GREEN (mínimo código para pasar)
- [x] Evidencia REFACTOR (sin romper pruebas)
- [ ] Historial de commits muestra la secuencia
- [x] TDD realizado en la iteración (sin commits atómicos por fase)

**Evidencia:**
- [ ] Commits referenciados
- [x] Nota explicativa por iteración

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
- Estado actual: **Parcialmente conforme**
- Riesgos abiertos: _No detección de tests en `ReservationService.Domain.Tests` y `ReservationService.Infrastructure.Tests`; falta evidencia visual (capturas)._ 
- Acción siguiente: adjuntar capturas de suites en verde y mantener declaración explícita de alcance de pruebas en ReservationService.
