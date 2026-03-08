# Definition of Ready (DoR) de TicketRush

## Propósito

Este documento define las condiciones mínimas para que una historia, requerimiento o bugfix pueda entrar a desarrollo en TicketRush sin introducir ambigüedad técnica en los flujos distribuidos de eventos, reservas y pagos.

## Alcance

Aplica a cambios de frontend, APIs, workers, topología de eventos, esquema de datos y automatización operativa del repositorio.

## 1) Criterios de negocio y semántica (obligatorios)

Una historia está Ready cuando:

1. Usa términos canónicos del dominio TicketRush.
2. Diferencia explícitamente entre reservar, pagar, liberar, expirar y consultar estado.
3. Identifica con claridad el actor o servicio que inicia el flujo.
4. Explica el resultado observable esperado para el usuario o consumidor técnico.
5. No deja ambigüedades sobre si el flujo es síncrono o asíncrono.

## 2) Criterios de aceptación (BDD) (obligatorios)

1. Incluye criterios verificables en formato BDD o equivalente estructurado.
2. Cubre al menos el escenario feliz y el principal escenario de error o conflicto.
3. Si el flujo es asíncrono, define explícitamente cómo se confirma el estado final.
4. Si hay TTL, concurrencia o idempotencia, esos escenarios deben quedar en los criterios.

## 3) Contratos y eventos (obligatorios cuando apliquen)

Antes de desarrollo debe quedar explícito, según el caso:

1. Endpoint HTTP impactado y contrato mínimo de request/response.
2. Routing key, cola y servicio consumidor si el cambio afecta RabbitMQ.
3. Estados persistidos y transiciones esperadas si se modifica ciclo de vida de ticket/pago.
4. Impacto en frontend si cambia el contrato consumido por `frontend/lib/api.ts`.
5. Impacto en `scripts/schema.sql` si cambia persistencia.

## 4) Alineación técnica y arquitectónica (obligatorios)

1. La historia identifica el servicio dominante: `frontend`, `producer`, `crud_service`, `ReservationService`, `paymentService` o `scripts`.
2. Respeta la separación entre lectura síncrona y comando asíncrono.
3. No contradice la topología centralizada en `scripts/setup-rabbitmq.sh`.
4. Si toca reservas, reconoce la restricción de optimistic locking.
5. Si toca pagos, reconoce validación de TTL, idempotencia y transiciones de estado.

## 5) Criterios de preparación para ejecución (obligatorios)

1. La historia es estimable sin suposiciones críticas abiertas.
2. Las dependencias entre servicios están identificadas.
3. Los riesgos relevantes están explicitados: concurrencia, asincronía, contrato, esquema o infraestructura.
4. El alcance deja claro qué queda dentro y fuera de la iteración.
5. Si el cambio es transversal, existe justificación de por qué no debe descomponerse.

## 6) Artefactos mínimos requeridos para declarar Ready

- [ ] Descripción refinada del cambio o historia.
- [ ] Terminología validada contra el diccionario de dominio.
- [ ] Criterios de aceptación verificables.
- [ ] Contratos HTTP/eventos afectados, si aplica.
- [ ] Servicios y archivos principales impactados identificados.
- [ ] Riesgos y dependencias documentados.
- [ ] Validación del equipo sobre el alcance.

## 7) Reglas de decisión Ready / Not Ready

### Ready

Un cambio está **Ready** cuando puede implementarse sin abrir decisiones fundamentales durante el desarrollo.

### Not Ready

Un cambio está **Not Ready** si ocurre cualquiera de estas condiciones:

1. No está claro si el resultado es inmediato o eventual.
2. No se sabe qué servicio debe cambiar primero.
3. Falta el contrato HTTP, evento o transición de estado afectada.
4. Hay ambigüedad sobre cómo se valida éxito, rechazo, expiración o concurrencia.
5. El impacto transversal es demasiado amplio y no está particionado.

## 8) Relación DoR vs DoD

- **DoR** valida la calidad de entrada al desarrollo.
- **DoD** valida la completitud y trazabilidad de salida.

Ningún cambio debería iniciar implementación si todavía obliga al equipo a descubrir contratos básicos durante la ejecución.
