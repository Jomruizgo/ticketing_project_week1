# Matriz Global de Flujo, Riesgo y Cobertura — TicketRush

| Flujo | Riesgo dominante | Cobertura actual identificada | Nivel actual | Gap principal |
|---|---|---|---|---|
| FL-001 Gestión de eventos | ruptura CRUD básica | `frontend/lib/api.ts`, documentación general | Media | falta suite explícita de integración documentada |
| FL-002 Gestión de tickets | inventario inconsistente | `frontend/lib/api.ts`, documentación general | Media | falta trazabilidad formal a pruebas automatizadas por API |
| FL-003 Reserva exitosa | doble reserva / pérdida de consistencia | `ProcessReservationCommandHandlerTests.cs`, `TicketRepository.cs`, documentación E2E | Alta | alinear frontend con SSE si persiste polling legado |
| FL-004 Reserva fallida por concurrencia | sobreventa | `TicketRepository.cs`, tests de ReservationService | Alta | reforzar smoke específico en PR |
| FL-005 Pago aprobado | ticket no pasa a `paid` o pago inconsistente | `PaymentValidationServiceTests.cs`, `TicketStateService.cs` | Alta | incluir gate más visible en CI |
| FL-006 Pago rechazado | ticket no se libera | `PaymentRejectedEventHandlerTests.cs`, `PaymentValidationServiceTests.cs` | Alta | sumar verificación E2E reproducible si aún no está fija |
| FL-007 Pago tardío / TTL | venta inválida o liberación incorrecta | `PaymentValidationService.cs`, `TESTING_STRATEGY.md` | Media-Alta | reforzar pruebas sobre la ruta canónica de expiración por RabbitMQ |
| FL-008 SSE estado ticket | frontend no recibe estado final | `SseContractIntegrationTests.cs`, `TicketStatusPipelineTests.cs`, hooks frontend | Alta | congelar contrato canónico |
| FL-009 Idempotencia eventos | doble efecto por reentrega | Payment tests + docs semana 3 | Alta | formalizar como check obligatorio de cambios en workers |
| FL-010 Expiración automática | ticket queda bloqueado o se libera incorrectamente | `verify-devA-expiration.sh`, docs actividad, configuración RabbitMQ | Media | convertir la verificación RabbitMQ en gate E2E estable y terminar de limpiar referencias históricas |

## Lectura de la matriz

- **Alta**: hay evidencia sólida de pruebas o controles implementados.
- **Media**: hay cobertura parcial, documentación o evidencia indirecta.
- **Baja**: el flujo requiere fortalecimiento antes de convertirse en gate serio.

## Prioridades de fortalecimiento

1. FL-010 Expiración automática.
2. FL-003 Reserva exitosa con consolidación SSE end-to-end.
3. FL-001 y FL-002 CRUD administrativo.
4. Cobertura de regresión formal por cambios transversales en estados.
