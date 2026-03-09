# Principios de Estructura del Proyecto TicketRush

## Organización física esperada

La estructura del repositorio está orientada por servicio y responsabilidad operativa:

- `frontend/` → aplicación web Next.js.
- `producer/` → API de comandos asíncronos.
- `crud_service/` → API síncrona de lectura/administración.
- `ReservationService/` → worker de reservas.
- `paymentService/` → worker de pagos.
- `scripts/` → topología RabbitMQ, esquema SQL y automatización operativa.

Dentro de los servicios .NET se espera separación en capas o proyectos con responsabilidades explícitas, por ejemplo:

- `Api`
- `Application`
- `Domain`
- `Infrastructure`
- `Worker`
- `Tests`

## Convenciones de cohesión

1. Una historia debe impactar el menor número posible de servicios.
2. Los cambios transversales deben estar justificados por contratos reales compartidos.
3. Cuando un cambio toca integración entre servicios, deben revisarse productor, consumidor y cliente afectado.
4. `frontend/lib/api.ts` es el punto canónico de integración HTTP del frontend; no duplicar clientes ad hoc.
5. `scripts/setup-rabbitmq.sh` es el punto canónico de topología RabbitMQ; no replicar bindings sin motivo explícito.

## Límites de dispersión para una historia de sprint

- Umbral recomendado de impacto principal: **máximo 3 componentes mayores** entre servicios o capas de alto nivel.
- Si un cambio obliga a tocar `producer` + `ReservationService` + `paymentService` + `frontend`, debe reevaluarse como iniciativa transversal o dividirse.
- Los cambios de esquema, contrato y evento en una sola historia deben justificarse con trazabilidad clara.

## Reglas prácticas de impacto

- Si cambia un endpoint consumido por el frontend, revisar `frontend/lib/api.ts`, tipos asociados y llamadas UI.
- Si cambia un evento RabbitMQ, revisar `producer`, `scripts/setup-rabbitmq.sh` y el/los workers consumidores.
- Si cambia el modelo de persistencia, revisar `scripts/schema.sql` y los mapeos/entidades de los servicios afectados.
- Si cambia un estado de ticket o pago, revisar frontend, consultas, workers e historial.

## Nomenclatura

- Mantener nombres de carpetas y proyectos coherentes con la responsabilidad real del servicio.
- Evitar carpetas temporales o nombres genéricos sin significado de dominio.
- No introducir nuevas estructuras transversales globales sin necesidad fuerte.

## Señales de épica disfrazada

- Requiere tocar múltiples servicios sin una única razón de negocio cohesionada.
- Introduce nuevos eventos, nuevos contratos HTTP y cambios de esquema simultáneamente sin un alcance bien delimitado.
- Obliga a modificar frontend, producer, uno o más workers y scripts de infraestructura en una sola iteración sin partición clara.
