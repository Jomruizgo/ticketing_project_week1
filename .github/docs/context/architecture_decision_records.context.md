# Architecture Decision Records (ADRs) de TicketRush

## Propósito

Este documento resume las decisiones arquitectónicas activas del proyecto para evitar cambios que contradigan la implementación real del sistema distribuido.

## Estados permitidos

- **Activa**: decisión vigente y obligatoria.
- **Obsoleta**: reemplazada por una decisión posterior.
- **En evaluación**: propuesta aún no adoptada.

## Registro de decisiones

| ID | Decisión | Estado | Fecha | Reemplaza |
| --- | --- | --- | --- | --- |
| ADR-001 | El frontend consume lectura síncrona desde `crud_service` y comandos asíncronos vía `producer` | Activa | 2026-03-07 | — |
| ADR-002 | `producer` responde `202 Accepted` para reserva y pago; el resultado de negocio se confirma después | Activa | 2026-03-07 | — |
| ADR-003 | RabbitMQ usa exchange topic `tickets` con topología centralizada en `scripts/setup-rabbitmq.sh` | Activa | 2026-03-07 | — |
| ADR-004 | La reserva debe preservar optimistic locking con filtro por `version` + `status` | Activa | 2026-03-07 | — |
| ADR-005 | `paymentService` maneja idempotencia, TTL y transición transaccional de estado | Activa | 2026-03-07 | — |
| ADR-006 | PostgreSQL usa enums en minúscula para `ticket_status` y `payment_status` | Activa | 2026-03-07 | — |
| ADR-007 | El frontend confirma resultados por observación posterior del estado, no por éxito inmediato del comando | Activa | 2026-03-07 | — |
| ADR-008 | Los cambios de contrato/evento/esquema deben propagarse a todos los componentes consumidores | Activa | 2026-03-07 | — |

## Detalle de ADRs activos

### ADR-001: Separación lectura/comando por servicio

**Contexto**

El sistema mezcla operaciones administrativas y flujos asíncronos de negocio. Necesita una frontera clara entre lectura y comando.

**Decisión**

- `crud_service` concentra lectura y administración síncrona.
- `producer` recibe comandos asíncronos de reserva y pago.

**Consecuencias**

- El frontend debe conocer dos superficies HTTP con roles distintos.
- Los cambios funcionales suelen implicar revisar ambos servicios solo cuando cruzan lectura y comando.

### ADR-002: Respuesta `202 Accepted` en comandos asíncronos

**Contexto**

La reserva y el pago no se resuelven en la misma transacción HTTP; dependen de RabbitMQ y workers.

**Decisión**

El `producer` devuelve `202 Accepted` para reserva y pago cuando el mensaje fue aceptado para procesamiento.

**Consecuencias**

- El frontend no puede tratar esa respuesta como confirmación final.
- Las pruebas deben validar aceptación inicial y resolución posterior por estado.

### ADR-003: Topología RabbitMQ centralizada

**Contexto**

Duplicar exchange, queues y bindings en varios puntos genera deriva operativa.

**Decisión**

La topología de RabbitMQ se define y mantiene en `scripts/setup-rabbitmq.sh`.

**Consecuencias**

- Todo cambio de routing key o cola debe pasar por ese archivo.
- Los servicios deben consumir configuración, no redefinir la topología por su cuenta.

### ADR-004: Reserva con optimistic locking

**Contexto**

El sistema debe evitar doble reserva bajo concurrencia.

**Decisión**

`ReservationService` reserva tickets usando una actualización atómica filtrada por `id`, `version` actual y `status = available`.

**Consecuencias**

- Quitar cualquiera de esas condiciones rompe la protección de concurrencia.
- Las pruebas de reserva deben incluir colisión concurrente.

### ADR-005: Pago con validación de idempotencia y TTL

**Contexto**

Los eventos de pago pueden repetirse o llegar fuera de la ventana válida de reserva.

**Decisión**

`paymentService` valida estado actual, idempotencia y TTL antes de confirmar `paid` o liberar el ticket.

**Consecuencias**

- Un pago tardío puede terminar en liberación en vez de confirmación.
- Los workers deben diferenciar resultados de negocio de errores técnicos.

### ADR-006: Enums persistidos en minúscula

**Contexto**

La base de datos usa enums PostgreSQL y distintos servicios representan estados con convenciones propias.

**Decisión**

La persistencia se alinea al esquema PostgreSQL en minúscula: `available`, `reserved`, `paid`, `released`, `cancelled`, `pending`, `approved`, `failed`, `expired`.

**Consecuencias**

- No se deben normalizar estados “por estética” sin revisar mappings.
- Cualquier cambio de enum exige revisar DB, servicios y frontend.

### ADR-007: Confirmación por estado observable

**Contexto**

El flujo de UX debe convivir con consistencia eventual.

**Decisión**

La confirmación real para el usuario proviene del estado posterior del ticket/pago consultado desde el sistema, no de la request inicial.

**Consecuencias**

- Los hooks y pantallas del frontend deben modelar estados intermedios.
- QA debe probar timeout, reintento y confirmación tardía.

### ADR-008: Propagación obligatoria de cambios de contrato

**Contexto**

El repo contiene varios servicios acoplados por contratos HTTP, eventos y esquema compartido.

**Decisión**

Todo cambio de contrato debe actualizar productor, consumidor y cliente impactado antes de considerarse completo.

**Consecuencias**

- No basta con compilar un solo servicio.
- La revisión técnica debe validar impacto transversal.

## Reglas de uso en análisis de requerimientos

1. Ningún análisis puede contradecir ADRs activos.
2. Si un requerimiento exige excepción, debe proponerse un nuevo ADR o una actualización explícita.
3. Los ADRs obsoletos solo sirven como trazabilidad histórica.

## Plantilla para nuevas decisiones

```markdown
### ADR-XXX: [Título]

**Estado**: En evaluación | Activa | Obsoleta
**Fecha**: YYYY-MM-DD
**Reemplaza**: ADR-YYY (opcional)

**Contexto**
[Problema o tensión arquitectónica]

**Decisión**
[Decisión adoptada]

**Consecuencias**
[Impactos positivos, trade-offs, riesgos]
```
