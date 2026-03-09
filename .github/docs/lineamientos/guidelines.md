# Lineamientos Generales del Ecosistema de Agentes

## Propósito

Este documento define reglas transversales para cualquier agente o prompt del framework ASD/GAIDD aplicado a TicketRush.

## 1. Principios rectores

1. **El código real manda**: si documentación y código difieren, prevalece el código real del repositorio.
2. **Cambios mínimos y trazables**: no rehacer componentes completos si el cambio es local.
3. **Contexto antes que implementación**: identificar primero servicio, contrato y flujo impactado.
4. **Nada de suposiciones silenciosas**: si el comportamiento asíncrono, un contrato o un estado no están claros, debe documentarse.
5. **Evolución sobre greenfield por defecto**: una HU nueva debe tratarse como cambio incremental sobre el sistema existente salvo que el usuario indique explícitamente que se implementa algo totalmente nuevo.

## 2. Reglas de trabajo sobre TicketRush

1. `frontend`, `producer`, `crud_service`, `ReservationService`, `paymentService` y `scripts` son los límites principales del repo.
2. `frontend/lib/api.ts` es el cliente HTTP canónico del frontend.
3. `scripts/setup-rabbitmq.sh` es la fuente de verdad de la topología RabbitMQ.
4. `scripts/schema.sql` es la base del esquema relacional compartido.
5. Reserva y pago deben tratarse como flujos asíncronos salvo evidencia explícita en contrario.
6. Si una HU nueva omite impactos sobre comportamiento previo, el agente debe inferirlos mediante análisis del código, contratos y tests existentes.

## 3. Reglas de impacto transversal

Cuando un cambio toca alguno de estos ejes, deben revisarse todos los consumidores afectados:

- **Contrato HTTP** → backend expuesto + frontend consumidor.
- **Evento RabbitMQ** → publisher + topología + consumer.
- **Esquema o enums** → `schema.sql` + servicios .NET + frontend que interpreta estado.
- **UX asíncrona** → hooks de seguimiento + componentes de UI + APIs de consulta.
- **Cambio funcional sobre flujo existente** → código actual + tests actuales + documentación viva + regresión esperada.

## 4. Git y entrega

1. Seguir GitFlow sobre `develop` del fork, salvo instrucción explícita distinta.
2. Usar Conventional Commits.
3. Si el usuario pide TDD, respetar estrictamente `RED -> GREEN -> REFACTOR`.

## 5. Salida esperada de los agentes

1. Los análisis deben identificar claramente el componente dominante.
2. Las recomendaciones deben ser accionables y específicas.
3. Los outputs deben guardarse en `.github/docs/output/` y no en rutas paralelas obsoletas.
4. Ningún agente debe asumir greenfield si ya existe una implementación parcial o total del flujo en el repositorio.
