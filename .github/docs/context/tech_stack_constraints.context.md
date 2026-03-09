# Restricciones de Stack Tecnológico TicketRush

## Frontend aprobado

- Next.js `16.1.6`
- React `19.2.3`
- TypeScript `5.7.3`
- Tailwind CSS `3.4.x`
- Componentes UI basados en Radix, utilidades tipo `zod`, `react-hook-form`, `swr`

## Backend aprobado

- .NET `8.0`
- ASP.NET Core Web API para `producer` y `crud_service`
- Worker Services en .NET para `ReservationService` y `paymentService`
- Entity Framework Core `8.x` / Npgsql en los servicios que usan EF
- RabbitMQ.Client `6.8.x`

## Persistencia y mensajería aprobadas

- PostgreSQL `15`
- RabbitMQ `3.12`
- Docker Compose como entorno operativo de referencia

## API y contratos

- APIs REST JSON sobre HTTP
- Endpoints de health para servicios HTTP
- `producer` usa respuestas `202 Accepted` para comandos asíncronos de reserva y pago
- `crud_service` expone endpoints síncronos para consulta y administración

## Tecnologías o enfoques no aprobados para este proyecto

- Sustituir RabbitMQ por otro broker sin instrucción explícita
- Crear una segunda fuente de verdad para topología de colas fuera de `scripts/setup-rabbitmq.sh`
- Reemplazar PostgreSQL por motores distintos sin cambio arquitectónico explícito
- Introducir clientes frontend alternos que dupliquen la integración canónica definida en `frontend/lib/api.ts`

## Restricciones de diseño y antipatrones prohibidos

- No asumir procesamiento síncrono en `producer` para operaciones que el sistema modela como asíncronas.
- No romper el contrato de consistencia eventual del frontend sin actualizar sus hooks/cliente.
- No duplicar declaraciones de exchanges, queues o bindings en múltiples servicios sin necesidad real.
- No eliminar el filtro por `version` + `status` en la reserva optimista.
- No normalizar ciegamente enums/estados entre bounded contexts; respetar el modelo real de cada servicio.
- No exponer secretos o credenciales en código fuente, documentación o logs.

## Capacidades y límites relevantes

- El sistema soporta creación y consulta síncrona de eventos/tickets desde `crud_service`.
- El sistema soporta reserva y pago asíncronos mediados por RabbitMQ.
- Los workers actualizan el estado observable en PostgreSQL y el frontend confirma por lecturas posteriores del estado.
- Los cambios de contratos, eventos o esquema requieren coordinación entre múltiples componentes del repo.
