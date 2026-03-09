# Estándares de Arquitectura del Proyecto TicketRush

## Propósito

Este documento define los patrones arquitectónicos aprobados, los antipatrones prohibidos y las reglas estructurales que deben respetarse al analizar, diseñar o implementar cambios en TicketRush.

## 1) Patrones arquitectónicos aprobados

1. **Separación por servicio y responsabilidad**:
   - `frontend`
   - `producer`
   - `crud_service`
   - `ReservationService`
   - `paymentService`
2. **Arquitectura asíncrona orientada a eventos** para reserva y pago.
3. **Capas explícitas** dentro de servicios .NET cuando el proyecto lo soporte:
   - `Api`
   - `Application`
   - `Domain`
   - `Infrastructure`
   - `Worker`
4. **Mensajería centralizada en RabbitMQ** con topología gestionada por scripts operativos del repositorio.
5. **Fuente de verdad persistente en PostgreSQL** con estados explícitos e historial de cambios.
6. **Cliente frontend canónico** centralizado en `frontend/lib/api.ts`.

## 2) Reglas de responsabilidad por componente

### `producer`

- Recibe comandos HTTP.
- Valida payload de entrada.
- Publica eventos y responde `202 Accepted` cuando el flujo es asíncrono.
- No debe cerrar el resultado final del negocio en la misma request.

### `crud_service`

- Expone lectura y administración síncrona.
- Es el punto de consulta de eventos, tickets y estados desde el frontend.
- Debe mantener contratos HTTP coherentes con el cliente web.

### `ReservationService`

- Procesa reservas consumiendo eventos.
- Debe preservar control de concurrencia optimista sobre tickets.
- No debe degradar la protección contra doble reserva.

### `paymentService`

- Procesa pagos y transiciones de estado.
- Debe manejar idempotencia, TTL y consistencia transaccional.
- Debe distinguir fallos de negocio de fallos técnicos.

### `frontend`

- Consume únicamente contratos publicados por las APIs del proyecto.
- Debe modelar estados intermedios, errores y consistencia eventual.
- No debe asumir éxito inmediato tras una respuesta `202`.

### `scripts/`

- Define infraestructura lógica compartida del entorno local y de integración.
- `setup-rabbitmq.sh` es la fuente de verdad para exchange, queues y bindings.
- `schema.sql` es la base del contrato relacional inicial.

## 3) Principios de acoplamiento y cohesión

1. El frontend depende de APIs, no de detalles internos de workers.
2. Los workers dependen de contratos de eventos y persistencia, no del frontend.
3. Las reglas de negocio deben vivir en servicios/casos de uso, no en controladores o scripts.
4. Los cambios deben concentrarse en el bounded context dominante de la historia.
5. Toda integración entre servicios debe estar respaldada por contratos explícitos: HTTP, eventos o esquema.

## 4) Antipatrones prohibidos

1. Resolver reserva o pago de forma síncrona en `producer` cuando el modelo del sistema es asíncrono.
2. Duplicar la topología RabbitMQ en múltiples ubicaciones del repo.
3. Eliminar o debilitar el filtro de `status` + `version` en la reserva optimista.
4. Romper el cliente canónico del frontend creando llamadas HTTP duplicadas y no trazables.
5. Fugar errores internos de infraestructura a contratos públicos sin traducción apropiada.
6. Modificar estados o enums sin revisar todos los servicios y el esquema compartido.
7. Introducir cambios transversales sin actualizar consumidores, productores y documentación afectada.

## 5) Decisiones de diseño vigentes (resumen operacional)

1. El flujo de reserva y pago se modela como **comando HTTP + procesamiento asíncrono**.
2. El frontend debe **confirmar el resultado por observación posterior del estado**, no por la aceptación inicial.
3. RabbitMQ usa un **exchange topic `tickets`** con routing keys explícitas del dominio.
4. El esquema relacional usa enums PostgreSQL en minúscula para estados persistidos.
5. Los contratos deben mantenerse alineados con `compose.yml`, `scripts/setup-rabbitmq.sh`, `scripts/schema.sql` y el cliente frontend.

## 6) Criterios de cumplimiento para análisis de requerimientos

Una propuesta se considera alineada cuando:

- [ ] Identifica el servicio o bounded context dominante.
- [ ] Respeta el carácter asíncrono de reservas y pagos.
- [ ] No duplica infraestructura lógica ya centralizada en `scripts/`.
- [ ] Conserva integridad de contratos HTTP/eventos/esquema.
- [ ] No contradice ADRs activos del proyecto.

## 7) Regla de gobernanza

Si un requerimiento exige desviarse de estos estándares:

1. se documenta la excepción,
2. se explicita el impacto en servicios y contratos,
3. se formaliza mediante ajuste de ADR o decisión arquitectónica aprobada.
