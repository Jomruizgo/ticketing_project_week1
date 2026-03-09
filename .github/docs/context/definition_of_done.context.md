# Definition of Done (DoD) de TicketRush

## Propósito

Este documento define las condiciones mínimas para considerar terminado un cambio en TicketRush, incluyendo cambios de frontend, APIs, workers, mensajería y persistencia.

## 1) Criterios de negocio y requerimiento

Un cambio se considera Done cuando:

1. El comportamiento final coincide con la intención funcional aprobada.
2. La terminología usada en código, documentación y pruebas respeta el dominio TicketRush.
3. Los estados finales esperados del ticket o pago son verificables.
4. No quedan decisiones funcionales críticas abiertas.

## 2) Criterios funcionales de contrato

Si el cambio afecta APIs o eventos, está Done cuando:

1. El contrato afectado quedó implementado y alineado entre productor y consumidor.
2. El frontend o servicio cliente afectado fue actualizado.
3. Las respuestas síncronas y los resultados asíncronos están correctamente diferenciados.
4. Los códigos HTTP, payloads y estados de error son coherentes con el flujo real.

## 3) Criterios técnicos y arquitectura

La implementación está Done cuando:

1. Respeta la separación por servicio y responsabilidad del repositorio.
2. No rompe la topología central de RabbitMQ.
3. Mantiene la protección de concurrencia en reserva cuando el flujo la involucra.
4. Mantiene idempotencia y manejo de TTL cuando el flujo toca pagos.
5. No introduce tecnologías o atajos fuera del stack aprobado.

## 4) Criterios de calidad de código

1. El código compila o construye en los componentes afectados.
2. No introduce duplicación evitable ni deuda técnica evidente en el área modificada.
3. El manejo de errores es consistente con la responsabilidad del componente.
4. Los cambios no rompen de forma silenciosa integraciones existentes.

## 5) Pruebas y validación

Un cambio está Done cuando existe evidencia verificable de prueba para lo impactado, incluyendo según aplique:

1. caso feliz,
2. caso de error o rechazo,
3. concurrencia o conflicto,
4. expiración/TTL,
5. contrato entre servicios,
6. impacto visible en frontend.

> El tipo de prueba exacto depende del cambio, pero un flujo distribuido no debe darse por terminado solo con prueba manual superficial.

## 6) Documentación y trazabilidad

1. Si cambió API, evento o esquema, la documentación relevante quedó actualizada.
2. Si cambió infraestructura lógica, se actualizaron `compose.yml`, `scripts/` o artefactos equivalentes cuando correspondía.
3. Queda trazabilidad entre el cambio, sus pruebas y el servicio afectado.
4. Si el cambio altera una decisión de arquitectura, debe registrarse en ADR o en el artefacto de análisis correspondiente.

## 7) Checklist operacional de cierre

- [ ] Contratos HTTP/eventos/esquema actualizados donde aplica.
- [ ] Servicios consumidores impactados ajustados.
- [ ] Frontend actualizado si cambió integración observable.
- [ ] Pruebas relevantes ejecutadas y revisadas.
- [ ] Sin ruptura conocida del flujo asíncrono de reserva o pago.
- [ ] Documentación y trazabilidad actualizadas.
- [ ] Revisión técnica completada.

## 8) Criterio de salida

Un cambio solo pasa a Done cuando el comportamiento final es demostrable y no deja inconsistencia conocida entre frontend, APIs, workers, RabbitMQ y PostgreSQL.
