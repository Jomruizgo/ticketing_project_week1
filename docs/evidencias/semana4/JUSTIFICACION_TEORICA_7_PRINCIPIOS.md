# Justificación teórica — Los 7 Principios de las Pruebas aplicados a TicketRush

Este documento complementa el [TEST_PLAN.md](docs/evidencias/semana4/TEST_PLAN.md) con una justificación teórica separada.

La decisión de mantenerlo aparte es intencional: el plan de pruebas debe concentrarse en alcance, riesgos, estrategia, suites y casos; la fundamentación académica puede vivir como soporte adicional sin recargar el artefacto operativo.

## 1. Principio 1 — Las pruebas muestran la presencia de defectos, no su ausencia

En TicketRush no basta con que un flujo “funcione en local”. Las pruebas solo reducen incertidumbre. Por eso se cubren estados críticos como:

- doble reserva,
- pagos duplicados,
- ventana de pago excedida,
- ruptura del contrato de notificación de estado,
- y divergencia entre evento aceptado y estado final.

## 2. Principio 2 — Las pruebas exhaustivas son imposibles

No se prueban todas las combinaciones posibles. Se priorizan flujos críticos y riesgos altos:

- reserva,
- pago,
- expiración,
- notificación de estado,
- y contratos inter-servicio.

## 3. Principio 3 — Las pruebas tempranas ahorran tiempo y dinero

La lógica crítica debe probarse desde capas bajas:

- `Unit` para guards y reglas,
- `Component` / `Integration` para contratos,
- `E2E` solo para validación final del flujo distribuido.

## 4. Principio 4 — Los defectos tienden a agruparse

Los riesgos se concentran en:

- cambios de estado del ticket,
- workers y mensajería,
- contrato de notificación de estado,
- y sincronización entre aceptación de comando y resultado final.

## 5. Principio 5 — Paradoja del pesticida

Si siempre se ejecutan las mismas pruebas, dejan de encontrar defectos nuevos. Por eso la estrategia exige:

- revisar regresión por flujo impactado,
- ampliar casos cuando entra una HU nueva,
- y revisar residuos técnicos o documentación no alineada.

## 6. Principio 6 — Las pruebas dependen del contexto

Este principio es central aquí. TicketRush no es un CRUD simple. Es un sistema distribuido, eventual-consistent y orientado a eventos. Por eso se prioriza:

- idempotencia,
- concurrencia,
- contratos de notificación,
- y verificación de integración entre servicios.

## 7. Principio 7 — La falacia de la ausencia de errores

No sirve tener muchas pruebas si no cubren los riesgos reales. Un backend puede “pasar tests” y seguir fallando si:

- una API acepta un comando pero no materializa correctamente el estado final,
- el estado no se expone correctamente al consumidor,
- o se rompe la mensajería que soporta el flujo distribuido.

## Conclusión

La utilidad de esta justificación no es decorativa. Sirve para explicar por qué el plan de pruebas prioriza riesgo, multicapas y comportamiento observable, en lugar de limitarse a ejecutar casos aislados sin criterio de arquitectura.