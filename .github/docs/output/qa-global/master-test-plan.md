# Plan Maestro de Pruebas Global — TicketRush

## 1. Propósito

Definir la estrategia global de calidad para el sistema TicketRush completo, tomando como fuente de verdad operativa el baseline reconstruido en `.github/docs/output/baseline/` y la evidencia de testing existente en el repositorio.

Este plan **no** está orientado a una HU puntual. Está orientado al producto actual y a su evolución incremental.

---

## 2. Objetivos de calidad

1. Proteger los flujos críticos de negocio del sistema distribuido.
2. Detectar regresiones funcionales cuando una HU nueva modifique comportamiento previo.
3. Validar contratos entre frontend, APIs HTTP, RabbitMQ y SSE.
4. Reducir el riesgo de fallos de concurrencia, idempotencia y consistencia eventual.
5. Definir una base objetiva para automatización de checks en PR.

---

## 3. Alcance

## 3.1 Dentro de alcance

- Gestión de eventos.
- Gestión de tickets.
- Reserva asíncrona.
- Pago asíncrono.
- Notificación de estado por SSE.
- Persistencia de estados e historial.
- Topología RabbitMQ relevante al negocio.
- Expiración/liberación de tickets.
- Contratos frontend ↔ backend ↔ mensajería.
- Cobertura automatizada actual y regresión futura.

## 3.2 Fuera de alcance por ahora

- Seguridad ofensiva profunda o pentesting.
- Performance masiva con carga alta sostenida.
- Auditoría regulatoria formal.
- Trazabilidad exacta a todas las HUs históricas originales.

---

## 4. Supuestos operativos

1. `producer` acepta comandos y devuelve `202 Accepted`, pero no decide el resultado final de negocio.
2. El estado final del ticket debe confirmarse por persistencia y/o SSE.
3. RabbitMQ y PostgreSQL forman parte del comportamiento real del sistema, no son detalles reemplazables sin impacto.
4. Una HU nueva puede modificar comportamiento previo aunque no lo declare explícitamente; por eso el plan incluye regresión obligatoria por flujo impactado.

---

## 5. Riesgos prioritarios del sistema

| Riesgo | Descripción | Severidad |
|---|---|---|
| R-001 | Doble reserva por pérdida del control de concurrencia | Crítica |
| R-002 | Aceptar `202` como éxito final en frontend | Alta |
| R-003 | Pago duplicado o reprocesado sin idempotencia | Crítica |
| R-004 | Ruptura del contrato SSE | Alta |
| R-005 | Divergencia entre topología RabbitMQ y consumidores reales | Alta |
| R-006 | Expiración/liberación inconsistente por regresión en la ruta canónica basada en RabbitMQ | Alta |
| R-007 | Cambio en estados persistidos que rompe frontend o workers | Alta |

---

## 6. Estrategia global de pruebas

## 6.1 Pirámide objetivo

### Unit
- Regla de negocio aislada.
- Validaciones de estado.
- Parsing/formatting.
- Transiciones y guards.

### Component
- Integración en memoria de piezas del mismo servicio.
- Pipeline de mensajería in-process cuando aplique.

### Integration
- Contratos HTTP.
- Contratos SSE.
- Persistencia real cuando el riesgo lo justifique.
- Integración entre componentes de infraestructura del mismo servicio.

### E2E
- Flujos completos de reserva, pago, liberación y consulta.
- Verificación sobre Docker Compose.
- Scripts reproducibles como evidencia del sistema real.

---

## 7. Flujos obligatorios a cubrir

| ID | Flujo | Prioridad |
|---|---|---|
| FL-001 | CRUD de eventos | Alta |
| FL-002 | CRUD/consulta de tickets | Alta |
| FL-003 | Reserva exitosa de ticket | Crítica |
| FL-004 | Reserva rechazada por concurrencia/no disponibilidad | Crítica |
| FL-005 | Pago aprobado dentro de TTL | Crítica |
| FL-006 | Pago rechazado | Crítica |
| FL-007 | Pago tardío / TTL excedido | Crítica |
| FL-008 | Notificación de estado por SSE | Crítica |
| FL-009 | Idempotencia ante reentrega de eventos | Crítica |
| FL-010 | Expiración/liberación automática de tickets | Crítica |

---

## 8. Cobertura mínima esperada por tipo de cambio

### Cambio solo frontend
- unit/UI tests del componente/hook impactado,
- regresión del contrato consumido,
- smoke del flujo visible afectado,
- verificación de estados asíncronos si interpreta tickets/pagos.

### Cambio solo backend HTTP
- unit de lógica,
- integración del endpoint,
- contrato con frontend consumidor,
- regresión de errores y serialización.

### Cambio en workers / RabbitMQ / estados
- unit de lógica de transición,
- integración del consumer/handler,
- regresión de eventos y estados,
- E2E del flujo distribuido afectado.

### Cambio transversal de flujo crítico
- smoke en PR,
- integración obligatoria,
- E2E del flujo completo antes de merge o por pipeline reforzado.

---

## 9. Suites globales recomendadas

## 9.1 Suite Smoke (PR obligatoria)
Debe completar rápido y dar señal temprana:

- build/lint básicos,
- tests unitarios existentes por servicio,
- tests de contrato SSE críticos,
- tests de command handlers del producer,
- tests críticos de ReservationService y PaymentService.

## 9.2 Suite Integración (PR selectiva o post-merge)

- `SseContractIntegrationTests`,
- `TicketStatusPipelineTests`,
- integraciones de consumers/handlers relevantes,
- validaciones de contratos HTTP según el cambio.

## 9.3 Suite E2E (merge/nightly/manual)

- `scripts/verify-e2e.sh`,
- `scripts/verify-devA-expiration.sh`,
- cualquier script adicional por flujo nuevo crítico.

---

## 10. Ambientes de prueba

| Ambiente | Uso |
|---|---|
| Local unit | desarrollo rápido y TDD |
| Local integration | contratos y componentes del servicio |
| Docker Compose local | E2E del sistema distribuido |
| CI PR | smoke + integración seleccionada |
| CI post-merge/nightly | E2E y verificaciones costosas |

---

## 11. Criterios de entrada y salida

## 11.1 Entrada para ejecutar pruebas globales

- baseline actualizado o aceptado,
- cambio identificado por flujo impactado,
- suites relevantes seleccionadas,
- entorno reproducible disponible.

## 11.2 Salida mínima aceptable

- sin fallos en suite smoke requerida,
- sin ruptura de contratos críticos,
- cobertura de regresión ejecutada sobre los flujos impactados,
- evidencias adjuntas para integraciones/E2E cuando aplique.

---

## 12. Bloqueadores actuales del plan

1. Alinear implementación y documentación con RabbitMQ como mecanismo canónico de expiración automática.
2. Eliminar o aislar el polling legado de reserva para consolidar SSE como mecanismo oficial de confirmación.
3. Ausencia de workflow CI consolidado en `.github/workflows/`.

---

## 13. Recomendaciones inmediatas

1. Congelar la versión 1 del baseline como insumo oficial del plan global.
2. Crear una matriz de cobertura flujo ↔ prueba ↔ riesgo.
3. Implementar GitHub Actions por capas:
   - PR: smoke + unit + contratos críticos,
   - post-merge/nightly: integración y E2E.
4. Actualizar este plan cuando entre una HU nueva que altere flujos existentes.
5. Registrar explícitamente en la documentación viva que:
   - la expiración oficial es por RabbitMQ,
   - la confirmación de reserva oficial es por SSE,
   - y cualquier polling residual debe tratarse como legado técnico.

---

## 14. Juicio final

El proyecto ya tiene suficiente evidencia para operar con un **plan maestro de pruebas global**. Lo que falta no es inventar estrategia desde cero, sino formalizarla y convertirla en gates automáticos y regresión gobernada.
