# Arquitectura del Proyecto TicketRush (Nivel Alto)

## Visión general

TicketRush es un sistema distribuido de ticketing con separación explícita entre:

- **Frontend web** para administración y compra.
- **Servicios HTTP síncronos** para lectura y publicación de comandos.
- **Workers asíncronos** para reserva y pago.
- **RabbitMQ** como backbone de mensajería.
- **PostgreSQL** como fuente de verdad del estado de eventos, tickets, pagos e historial.

El flujo funcional clave del producto es:

1. El frontend consulta datos en `crud_service`.
2. El frontend envía comandos de reserva o pago al `producer`.
3. El `producer` responde `202 Accepted` y publica eventos en RabbitMQ.
4. `ReservationService` y `paymentService` procesan de forma asíncrona y actualizan estado en base de datos.
5. El frontend confirma el resultado vía lectura posterior del estado (polling y/o mecanismos de actualización según el flujo implementado).

## Estilo arquitectónico

- **Arquitectura distribuida orientada a eventos**.
- **Separación CQRS liviana por responsabilidad**:
	- escritura de comandos asíncronos vía `producer`,
	- lectura y administración síncrona vía `crud_service`.
- **Servicios .NET en capas** para APIs y workers.
- **Frontend Next.js** como cliente único del MVP.

## Bounded contexts operativos

### 1. Gestión de Eventos y Tickets

Responsable de:

- crear eventos,
- crear tickets,
- consultar disponibilidad,
- exponer estado actual al frontend.

Componentes principales:

- `crud_service`
- `frontend`

### 2. Reserva de Tickets

Responsable de:

- recibir intención de reserva,
- validar disponibilidad,
- reservar con control de concurrencia,
- definir expiración de reserva.

Componentes principales:

- `producer`
- `ReservationService`

### 3. Pagos

Responsable de:

- recibir solicitud de pago,
- evaluar aprobación o rechazo,
- actualizar ticket y pago,
- registrar historial.

Componentes principales:

- `producer`
- `paymentService`

### 4. Mensajería y Topología

Responsable de:

- exchange, colas y bindings,
- enrutamiento entre publisher y workers,
- contratos de eventos.

Componentes principales:

- `scripts/setup-rabbitmq.sh`
- `compose.yml`
- configuración RabbitMQ de cada servicio

## Componentes principales y responsabilidades

### `frontend/`

- Aplicación Next.js para administración y compra.
- Usa `frontend/lib/api.ts` como cliente canónico.
- Debe asumir consistencia eventual en reservas y pagos.

### `producer/`

- API HTTP de entrada para comandos asíncronos.
- Publica `ticket.reserved` y `ticket.payment.requested`.
- No resuelve negocio final de reserva/pago en la respuesta HTTP.

### `crud_service/`

- API síncrona de lectura/escritura administrativa.
- Gestiona eventos, tickets, estados y consultas desde el frontend.
- Es la superficie de consulta del estado observable del sistema.

### `ReservationService/`

- Worker responsable de materializar reservas.
- Usa control de concurrencia optimista para evitar doble reserva.

### `paymentService/`

- Worker responsable de validar y aplicar pagos.
- Gestiona idempotencia, TTL y transiciones de estado.

### `scripts/`

- Contiene la topología central de RabbitMQ y utilitarios operativos.
- `setup-rabbitmq.sh` es la fuente de verdad de exchange, queues y bindings.
- `schema.sql` define el modelo relacional base del MVP.

## Restricciones arquitectónicas

1. El `producer` responde aceptación de comandos; el resultado de negocio ocurre después, no en la misma request.
2. La topología de RabbitMQ se centraliza en `scripts/setup-rabbitmq.sh`; no debe duplicarse sin una razón explícita.
3. La reserva debe preservar el control de concurrencia optimista basado en `status` + `version`.
4. Los cambios de estado de ticket y pago deben persistirse de forma consistente con historial y reglas del worker responsable.
5. El frontend no debe asumir confirmación inmediata de reserva o pago.
6. Cuando un cambio impacta contratos entre servicios, deben actualizarse productor, consumidor y cliente afectado.

## Criterios de encaje para requerimientos

- Cambios sobre creación/listado/edición de eventos o tickets suelen pertenecer a `crud_service` + `frontend`.
- Cambios sobre reserva pertenecen a `producer` + `ReservationService` + modelo de tickets.
- Cambios sobre pago pertenecen a `producer` + `paymentService` + modelo de pagos/tickets.
- Cambios que alteren routing keys, colas o eventos deben incluir `scripts/setup-rabbitmq.sh`.
- Cambios que crucen reserva, pago y frontend en una sola historia deben descomponerse si comprometen estimabilidad o trazabilidad.
