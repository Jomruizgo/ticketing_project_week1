# Matriz de Trazabilidad — Baseline TicketRush

## Leyenda de estado

- **Confirmado**: evidencia directa en código, tests, scripts o contratos.
- **Inferido**: evidencia múltiple pero no unívoca.
- **Pendiente**: contradicción o evidencia incompleta.

## Matriz

| ID | Requisito / capacidad | Estado | Evidencia de código / config | Evidencia de pruebas / docs | Servicios impactados |
|---|---|---|---|---|---|
| BR-F-001 | Listado y consulta de eventos | Confirmado | `frontend/lib/api.ts` | `README.md` | frontend, crud_service |
| BR-F-002 | Alta/edición/baja de eventos | Confirmado | `frontend/lib/api.ts` | `README.md` | frontend, crud_service |
| BR-F-003 | Listado/consulta de tickets | Confirmado | `frontend/lib/api.ts` | `README.md` | frontend, crud_service |
| BR-F-004 | Creación masiva de tickets | Confirmado | `frontend/lib/api.ts` (`/api/tickets/bulk`) | `README.md` | frontend, crud_service |
| BR-F-005 | Reserva asíncrona por RabbitMQ | Confirmado | `TicketsController.cs`, `compose.yml`, `scripts/setup-rabbitmq.sh` | `README.md` | frontend, producer, RabbitMQ, ReservationService |
| BR-F-006 | `202 Accepted` en reserva | Confirmado | `TicketsController.cs`, `frontend/lib/api.ts` | `README.md` | producer, frontend |
| BR-F-007 | Lock optimista de reserva | Confirmado | `TicketRepository.cs` | pruebas de ReservationService documentadas | ReservationService, PostgreSQL |
| BR-F-008 | Solicitud de pago asíncrona | Confirmado | `PaymentsController.cs`, `frontend/lib/api.ts`, `compose.yml` | `README.md` | frontend, producer, paymentService |
| BR-F-009 | Pago aprobado solo sobre ticket reservado | Confirmado | `PaymentValidationService.cs` | `PaymentValidationServiceTests.cs` | paymentService |
| BR-F-010 | Idempotencia en eventos de pago | Confirmado | `PaymentValidationService.cs`, `TicketStateService.cs` | `PaymentApprovedEventHandlerTests.cs`, `PaymentRejectedEventHandlerTests.cs` | paymentService |
| BR-F-011 | TTL vencido libera ticket | Confirmado | `PaymentValidationService.cs` | `TESTING_STRATEGY.md` | paymentService |
| BR-F-012 | Pago aprobado lleva ticket a `paid` | Confirmado | `TicketStateService.cs` | tests de payment y estrategia de testing | paymentService, PostgreSQL |
| BR-F-013 | Pago rechazado lleva ticket a `released` | Confirmado | `TicketStateService.cs` | tests de payment y estrategia de testing | paymentService, PostgreSQL |
| BR-F-014 | Propagación de `ticket.status.changed` | Confirmado | `scripts/setup-rabbitmq.sh`, arquitectura descrita | `SseContractIntegrationTests.cs`, `TicketStatusPipelineTests.cs` | ReservationService, paymentService, crud_service |
| BR-F-015 | Confirmación al frontend del estado final | Confirmado | hooks `use-payment-status.ts`, `use-ticket-status-sse.ts` | `README.md`, pruebas SSE documentadas | frontend, crud_service |
| BR-F-016 | Stream SSE por ticket | Confirmado | hooks frontend y contrato `/api/tickets/{id}/stream` | `SseContractIntegrationTests.cs`, `README.md` | frontend, crud_service |
| BR-F-017 | Historial de cambios de estado | Confirmado | `schema.sql`, `TicketStateService.cs` | documentación técnica | paymentService, PostgreSQL |
| BR-NF-001 | Separación lectura/comandos | Confirmado | `compose.yml`, `frontend/lib/api.ts` | `README.md` | sistema completo |
| BR-NF-002 | RabbitMQ exchange `tickets` como núcleo de integración | Confirmado | `scripts/setup-rabbitmq.sh`, `compose.yml` | `README.md` | sistema completo |
| BR-NF-003 | Enums nativos PostgreSQL para estados | Confirmado | `schema.sql` | `AI_WORKFLOW.md` | PostgreSQL, servicios .NET |
| BR-NF-004 | Consistencia observable basada en estado final, no en aceptación de comando | Confirmado | `frontend/lib/api.ts`, hooks frontend | `README.md` | frontend, producer |
| BR-NF-005 | Suite de pruebas multinivel | Confirmado | proyectos `*Tests.cs` | `TESTING_STRATEGY.md` | repo completo |
| BR-P-001 | Reserva usa polling o SSE según implementación canónica | Pendiente | `use-reservation-status.ts`, componentes frontend | `README.md` | frontend |
| BR-P-002 | Referencias documentales residuales sobre expiración pese a canon RabbitMQ ya definido | Pendiente de corregir | `scripts/setup-rabbitmq.sh`, `ReservationService/README.md` | docs actividad semana 3 | ReservationService, RabbitMQ, scripts |
| BR-P-003 | Relación exacta con HUs históricas originales | Pendiente | no hay carpeta viva de backlog consolidado | n/a | proceso/equipo |

## Observaciones de trazabilidad

1. La trazabilidad técnica es suficiente para QA de sistema.
2. La trazabilidad histórica a HUs originales no es suficiente para auditoría de backlog.
3. El siguiente artefacto recomendado es una matriz de cobertura de pruebas por flujo crítico y por riesgo.
