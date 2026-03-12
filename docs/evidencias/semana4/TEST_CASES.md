# TEST_CASES.md — Casos de Prueba Detallados TicketRush (Backend)

Este documento concentra los **casos de prueba detallados** referenciados por el [TEST_PLAN.md](docs/evidencias/semana4/TEST_PLAN.md).

Su objetivo es mantener separado:

- el **plan maestro** como artefacto ejecutivo de alcance, riesgos, estrategia y control,
- y los **casos de prueba** como artefacto operativo de ejecución y trazabilidad detallada.

---

## HU-R01 — Administración de eventos

### TC-010 — Administración de eventos
- **Flujo:** Administración
- **Nivel:** Integration
- **Tipo:** Caja Negra
- **Prioridad:** Alta
- **Precondiciones:** API de CRUD disponible.
- **Pasos:**
  1. Crear evento.
  2. Consultarlo.
  3. Actualizarlo.
  4. Eliminarlo.
- **Resultado esperado:** respuestas correctas y estado consistente.
- **Automatización:** Sí.

## HU-R02 — Disponibilidad de tickets por evento

### TC-011 — Disponibilidad inicial de tickets por evento
- **Flujo:** Inventario
- **Nivel:** Integration
- **Tipo:** Caja Negra
- **Prioridad:** Alta
- **Precondiciones:** evento existente.
- **Pasos:**
  1. Crear tickets en lote.
  2. Consultar tickets del evento.
- **Resultado esperado:** cantidad y estados iniciales correctos.
- **Automatización:** Sí.

## HU-R03 — Reserva correcta de un ticket

### TC-001 — Reserva aceptada y confirmada en el estado final
- **Flujo:** Reserva de ticket
- **Nivel:** E2E
- **Tipo:** Caja Negra
- **Prioridad:** Crítica
- **Precondiciones:** ticket disponible; servicios levantados; mecanismo de confirmación de estado operativo.
- **Pasos:**
  1. Consultar ticket disponible.
  2. Enviar `POST /api/tickets/reserve`.
  3. Verificar respuesta `202 Accepted`.
  4. Confirmar el estado final del ticket por el mecanismo oficial del sistema.
- **Resultado esperado:** el cliente de prueba recibe confirmación de que el ticket quedó `reserved`; no se toma el `202` como confirmación final.
- **Automatización:** Sí, objetivo de pipeline E2E.

### TC-002 — Reserva rechazada por concurrencia
- **Flujo:** Reserva concurrente
- **Nivel:** Unit / Integration
- **Tipo:** Caja Blanca
- **Prioridad:** Crítica
- **Precondiciones:** ticket disponible; dos intentos sobre mismo ticket.
- **Pasos:**
  1. Ejecutar reservas concurrentes sobre el mismo ticket.
  2. Observar resultado del repositorio/handler.
- **Resultado esperado:** solo una reserva se confirma; la otra falla sin doble asignación.
- **Automatización:** Sí.

### TC-008 — La solicitud aceptada no se considera completada hasta confirmar el resultado final
- **Flujo:** Confirmación del resultado final
- **Nivel:** Integration
- **Tipo:** Caja Negra
- **Prioridad:** Alta
- **Precondiciones:** endpoint asíncrono disponible; mecanismo oficial de consulta o confirmación operativo.
- **Pasos:**
  1. Ejecutar reserva o pago.
  2. Verificar respuesta `202 Accepted`.
  3. Confirmar después el estado final por el mecanismo definido por el sistema.
- **Resultado esperado:** el sistema distingue aceptación de solicitud vs resultado final real del negocio.
- **Automatización:** Sí.

## HU-R04 — Confirmación o liberación por pago

### TC-003 — Pago válido confirma la compra
- **Flujo:** Pago exitoso
- **Nivel:** Integration / E2E
- **Tipo:** Caja Negra
- **Prioridad:** Crítica
- **Precondiciones:** ticket reservado dentro del tiempo válido.
- **Pasos:**
  1. Solicitar pago.
  2. Procesar evento aprobado.
  3. Esperar confirmación del estado final.
- **Resultado esperado:** ticket en `paid`, payment en `approved`, historial registrado.
- **Automatización:** Sí.

### TC-004 — Pago rechazado libera ticket
- **Flujo:** Pago rechazado
- **Nivel:** Integration / E2E
- **Tipo:** Caja Negra
- **Prioridad:** Crítica
- **Precondiciones:** ticket reservado.
- **Pasos:**
  1. Solicitar pago.
  2. Procesar evento rechazado.
  3. Verificar estado final.
- **Resultado esperado:** ticket en `released`; el contrato backend expone notificación coherente.
- **Automatización:** Sí.

### TC-005 — Pago fuera de tiempo no confirma la compra
- **Flujo:** Validación temporal del pago
- **Nivel:** Unit / Integration
- **Tipo:** Caja Blanca
- **Prioridad:** Crítica
- **Precondiciones:** ticket reservado fuera del tiempo permitido.
- **Pasos:**
  1. Simular aprobación tardía.
  2. Ejecutar validación del worker.
- **Resultado esperado:** resultado de negocio fallido; transición a `released`; no queda ticket en `paid`.
- **Automatización:** Sí.

### TC-006 — Reentrega duplicada de evento aprobado
- **Flujo:** Idempotencia
- **Nivel:** Unit
- **Tipo:** Caja Blanca
- **Prioridad:** Crítica
- **Precondiciones:** evento aprobado ya procesado una vez.
- **Pasos:**
  1. Reprocesar el mismo evento.
- **Resultado esperado:** el sistema no duplica efectos ni altera indebidamente estado/historial.
- **Automatización:** Sí.

## HU-R05 — Consulta clara del estado final

### TC-007 — Confirmación clara del estado del ticket
- **Flujo:** Confirmación de estado
- **Nivel:** Integration
- **Tipo:** Caja Negra / contrato
- **Prioridad:** Crítica
- **Precondiciones:** mecanismo de confirmación de estado activo por ticket.
- **Pasos:**
  1. Provocar cambio de estado.
  2. Capturar la confirmación emitida por el sistema.
- **Resultado esperado:** la confirmación contiene la información esperada para informar correctamente el estado final.
- **Automatización:** Sí.

## HU-R06 — Liberación automática de tickets no concretados

### TC-009 — Un ticket no concretado vuelve a quedar disponible
- **Flujo:** Expiración
- **Nivel:** E2E / Integration
- **Tipo:** Caja Negra
- **Prioridad:** Crítica
- **Precondiciones:** ticket reservado con expiración configurada.
- **Pasos:**
  1. Provocar o esperar el evento oficial que libera el ticket no concretado.
  2. Observar cambio de estado.
- **Resultado esperado:** ticket liberado mediante la ruta oficial del sistema.
- **Automatización:** Sí.

### TC-012 — Historial de cambios de estado
- **Flujo:** Auditabilidad técnica
- **Nivel:** Integration
- **Tipo:** Caja Blanca
- **Prioridad:** Media-Alta
- **Precondiciones:** ejecutar transición de estado.
- **Pasos:**
  1. Forzar transición `reserved → paid` o `reserved → released`.
  2. Consultar persistencia de historial.
- **Resultado esperado:** registro correcto en `ticket_history` con razón y timestamps coherentes.
- **Automatización:** Sí.