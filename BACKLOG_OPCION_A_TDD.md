# Backlog detallado — Opción A (Expiración automática de reservas)

## 1) Diagnóstico técnico validado en el código

### Qué ya existe
- Topología RabbitMQ para expiración:
  - Cola delay `q.ticket.reserved.delay` con TTL + dead-letter a `ticket.expired`.
  - Cola destino `q.ticket.expired` y binding existentes.
- Flujo de reserva asíncrona operativo (`ticket.reserved` → ReservationService).
- Publicación de `ticket.status.changed` y relay SSE en `crud_service`.

### Qué falta para cerrar la feature A
- No existe consumer para `q.ticket.expired` en `ReservationService`.
- No existe caso de uso de expiración (estado `reserved -> released`).
- No existe `TryReleaseAsync` en el puerto `ITicketRepository` ni implementación en `TicketRepository`.
- Falta cobertura TDD del flujo de expiración y de la propagación SSE de `released`.

---

## 2) Historia de Usuario principal

### HU-EXP-01
**Como** plataforma de ticketing, **quiero** liberar automáticamente reservas vencidas, **para** que los tickets no queden bloqueados y vuelvan a estar disponibles.

### Criterios de aceptación (Gherkin)

```gherkin
Feature: Expiración automática de reservas

  Scenario: Ticket reservado expira y se libera
    Given un ticket en estado "reserved"
    And su TTL de reserva ya venció
    When ReservationService consume el evento "ticket.expired"
    Then el ticket transiciona a "released"
    And se publica "ticket.status.changed" con estado "released"

  Scenario: Evento duplicado de expiración
    Given un ticket que ya está en estado "released"
    When llega de nuevo el evento "ticket.expired"
    Then no hay nueva transición de estado
    And el procesamiento termina sin error

  Scenario: Ticket ya pagado
    Given un ticket en estado "paid"
    When llega un evento "ticket.expired"
    Then el ticket permanece en "paid"
    And no se publica un nuevo "ticket.status.changed"

  Scenario: Falla técnica de persistencia
    Given un ticket en estado "reserved"
    And ocurre una excepción técnica al liberar
    When se procesa "ticket.expired"
    Then el mensaje se maneja según política de fallo técnico
    And el ticket no cambia de estado
```

---

## 3) División entre 2 desarrolladores (sin conflictos)

## Dev A — Núcleo de negocio en `ReservationService`
**Rama:** `feature/expiry-core-reservation`

**Responsabilidad exclusiva**
- Nuevo consumer de expiración.
- Caso de uso de expiración.
- Puerto + repositorio para liberar con optimistic locking.
- Publicación de `status.changed` con `released`.
- Tests de Application/Infrastructure en ReservationService.

**Áreas/archivos que toma Dev A (ownership)**
- `ReservationService/src/ReservationService.Application/**`
- `ReservationService/src/ReservationService.Domain/**`
- `ReservationService/src/ReservationService.Infrastructure/**`
- `ReservationService/tests/**`

## Dev B — Contrato de propagación y capa de lectura (`crud_service`)
**Rama:** `feature/expiry-readmodel-sse`

**Responsabilidad exclusiva**
- Verificación y robustecimiento de consumo `ticket.status.changed` en `crud_service` para estado `released`.
- Suite de pruebas de `TicketStatusConsumer`/`TicketStatusHub`.
- Evidencia de integración de lectura y SSE.

**Áreas/archivos que toma Dev B (ownership)**
- `crud_service/Messaging/**`
- `crud_service/Services/**` (solo si se requiere ajuste menor de contrato)
- `crud_service/Controllers/**` (solo si se requiere ajuste menor de exposición)
- `crud_service` proyecto de tests nuevo o existente

## Reglas anti-conflicto
- No tocar el mismo archivo entre Dev A y Dev B en la misma iteración.
- Congelar contrato de evento al inicio (campos de `ticket.status.changed`).
- Integración por PRs pequeños y ordenados:
  1. PR Dev A (core de expiración)
  2. PR Dev B (lectura/SSE)
  3. PR conjunto de integración E2E

---

## 4) Backlog TDD por fases (Red → Green → Refactor)

## HU-EXP-01-A (Dev A)

### RED
- [ ] Crear tests de `ProcessExpirationCommandHandler`:
  - [ ] libera `reserved` correctamente.
  - [ ] no-op para `paid`.
  - [ ] no-op idempotente para `released`.
  - [ ] fallo técnico controlado.
- [ ] Crear test de repositorio para `TryReleaseAsync` con optimistic locking.
- [ ] Crear test de consumer para `q.ticket.expired` validando invocación al caso de uso.

### GREEN
- [ ] Crear `ProcessExpirationCommand`.
- [ ] Crear `IProcessExpirationUseCase`.
- [ ] Implementar `ProcessExpirationCommandHandler`.
- [ ] Extender `ITicketRepository` con `TryReleaseAsync`.
- [ ] Implementar `TryReleaseAsync` en `TicketRepository`.
- [ ] Implementar `TicketExpiredConsumer` (`BackgroundService`) para `q.ticket.expired`.
- [ ] Registrar servicios en `DependencyInjection`.
- [ ] Publicar `ticket.status.changed` con estado `released` al liberar.

### REFACTOR
- [ ] Limpiar duplicación entre consumer de reserva y consumer de expiración (sin romper boundaries hexagonales).
- [ ] Mejorar logs estructurados para diagnósticos de idempotencia/concurrencia.
- [ ] Reforzar nombres y factories de mensajes para legibilidad.

## HU-EXP-01-B (Dev B)

### RED
- [ ] Test de `TicketStatusConsumer` procesando `NewStatus = released`.
- [ ] Test de `TicketStatusHub.Notify` con suscriptor activo.
- [ ] Test de no-op seguro sin suscriptores.

### GREEN
- [ ] Ajustes mínimos de deserialización/contrato solo si falla con `released`.
- [ ] Crear o completar proyecto de pruebas de `crud_service` para mensajería/SSE.

### REFACTOR
- [ ] Unificar utilidades de payload para eventos de estado.
- [ ] Aislar creación de mensaje SSE para reducir fragilidad de tests.

## HU-EXP-01-INT (Ambos)

### RED
- [ ] Definir script/checklist E2E reproducible (reservar → esperar expiración → validar `released`).

### GREEN
- [ ] Ejecutar pruebas de `ReservationService` y `crud_service`.
- [ ] Validar flujo integrado con RabbitMQ y PostgreSQL local.

### REFACTOR
- [ ] Ajustes finales de estabilidad (timeouts/retries/logs) sin cambiar comportamiento.

---

## 5) Secuencia recomendada de ejecución (para evitar bloqueos)

1. **Día 1 (AM):** Alinear contrato de evento y matriz de estados.
2. **Día 1 (PM):** RED paralelo (Dev A y Dev B).
3. **Día 2:** GREEN paralelo con PRs pequeños.
4. **Día 3 (AM):** Merge de PR Dev A → `develop`.
5. **Día 3 (PM):** Rebase Dev B, merge de PR Dev B.
6. **Día 4:** Integración E2E + evidencia + PR final.

---

## 6) Definición de Ready (DoR)
- [ ] Contrato `ticket.expired` y `ticket.status.changed` acordado.
- [ ] Política de ACK/NACK para fallas técnicas definida.
- [ ] TTL objetivo confirmado (actual 5 min en infraestructura).
- [ ] Datos de prueba disponibles en entorno local.

## 7) Definición de Done (DoD)
- [ ] Todos los escenarios Gherkin de HU-EXP-01 en verde.
- [ ] Evidencia TDD visible en commits (RED, GREEN, REFACTOR).
- [ ] Sin conflictos funcionales con flujo de pago actual.
- [ ] Validación integrada RabbitMQ + DB + SSE completada.
- [ ] PR final a `develop` del fork con evidencia de pruebas.

---

## 8) Riesgos concretos y mitigación
- **Riesgo:** carrera entre pago aprobado y expiración.
  - **Mitigación:** state guard + optimistic locking (`status/version`) en `TryReleaseAsync`.
- **Riesgo:** doble procesamiento de mensajes.
  - **Mitigación:** idempotencia explícita en handler (no-op en estados no aplicables).
- **Riesgo:** incompatibilidad de payload entre servicios.
  - **Mitigación:** contrato congelado y test de contrato mínimo en ambos lados.
- **Riesgo:** conflictos de merge.
  - **Mitigación:** ownership por carpetas, PRs pequeños y rebase diario.
