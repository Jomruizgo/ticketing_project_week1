# Diccionario de Dominio de Negocio TicketRush

## Términos canónicos

| Término | Definición canónica | Sinónimos aceptados | Sinónimos rechazados | Ejemplo de uso correcto |
| --- | --- | --- | --- | --- |
| Evento | Unidad de oferta comercial sobre la que se venden tickets. | Show, concierto | Producto | "Crear un evento" |
| Ticket | Unidad reservable y pagable asociada a un evento. | Entrada | Cupo, producto | "Reservar un ticket" |
| Reserva | Bloqueo temporal de un ticket disponible para un comprador. | Hold | Compra confirmada | "La reserva expira en 5 minutos" |
| Pago | Proceso de aprobación o rechazo asociado a un ticket reservado. | Payment | Cobro definitivo (si no se confirma estado) | "Procesar el pago del ticket" |
| Estado del ticket | Situación actual del ticket en el flujo de negocio. | Status | Fase genérica | "El ticket quedó en estado reserved" |
| `available` | Ticket libre para ser reservado. | Disponible | Activo | "El ticket inicia en available" |
| `reserved` | Ticket retenido temporalmente para un comprador. | Reservado | Vendido | "El ticket quedó reserved" |
| `paid` | Ticket con pago aprobado y confirmado. | Pagado | Completado (ambiguo) | "El ticket pasó a paid" |
| `released` | Ticket liberado después de rechazo, expiración o reversión. | Liberado | Cancelado (si no hay anulación explícita) | "El ticket fue released" |
| `cancelled` | Ticket anulado por una regla o flujo explícito de negocio. | Cancelado | Liberado | "El ticket fue cancelled" |
| Estado del pago | Situación actual del registro de pago. | Payment status | Resultado | "El payment quedó approved" |
| `pending` | Pago creado o recibido pero aún no resuelto. | Pendiente | En proceso (si no es literal) | "El pago inicia en pending" |
| `approved` | Pago validado con éxito. | Aprobado | Exitoso | "El pago fue approved" |
| `failed` | Pago rechazado por validación o decisión de negocio. | Fallido, rechazado | Error genérico | "El pago quedó failed" |
| `expired` | Pago recibido fuera de la ventana válida de reserva. | Expirado | Vencido (si no se usa en código) | "El pago quedó expired" |
| TTL de reserva | Tiempo máximo durante el cual una reserva sigue siendo válida. | Ventana de reserva | Timeout genérico | "Validar TTL antes de aprobar pago" |
| `ticket.reserved` | Evento de integración que representa una solicitud de reserva aceptada para procesamiento. | Evento de reserva | Comando interno | "Publicar ticket.reserved" |
| `ticket.payment.requested` | Evento de integración que representa una solicitud de pago. | Solicitud de pago | Pago aprobado | "Publicar ticket.payment.requested" |
| `ticket.payments.approved` | Evento de integración de pago aprobado. | Pago aprobado | Confirmación genérica | "Consumir ticket.payments.approved" |
| `ticket.payments.rejected` | Evento de integración de pago rechazado. | Pago rechazado | Fallo técnico | "Consumir ticket.payments.rejected" |
| `ticket.status.changed` | Evento de integración que notifica transición observable del ticket. | Cambio de estado | Evento de dominio local | "Emitir ticket.status.changed" |
| Comprador | Persona o identificador que reserva o paga un ticket. | Buyer | Usuario genérico | "reservedBy identifica al comprador" |
| `order_id` | Referencia de negocio de la reserva. | Orden | Id técnico | "Persistir order_id al reservar" |
| Consistencia eventual | Modelo donde el resultado observable se confirma después del comando inicial. | Eventual consistency | Asincronía genérica | "El frontend debe asumir consistencia eventual" |
| Idempotencia | Capacidad de procesar eventos repetidos sin duplicar efectos de negocio. | Procesamiento seguro ante duplicados | Retry simple | "El worker debe ser idempotente" |

## Reglas semánticas del dominio

1. **Evento** y **ticket** son entidades distintas; un evento agrupa muchos tickets.
2. Una **reserva** no equivale a una venta cerrada; solo bloquea temporalmente el ticket.
3. Un ticket pasa a **paid** únicamente cuando el pago fue aprobado y persistido correctamente.
4. **released** se usa para liberar una reserva; no debe confundirse con **cancelled**.
5. Los nombres de eventos RabbitMQ deben usarse literalmente cuando se documenten contratos entre servicios.
6. La confirmación al usuario ocurre con base en el estado persistido, no solo por la aceptación HTTP inicial.

## Ambigüedades comunes a evitar

- "Comprar ticket" cuando el flujo real primero reserva y luego procesa pago.
- "Confirmado" sin especificar si se refiere a `reserved` o `paid`.
- "Expirado" sin aclarar si aplica al ticket o al pago.
- "Disponible" sin indicar si se habla del estado `available` del ticket.
- "Procesar pago" sin distinguir solicitud (`ticket.payment.requested`) de resolución (`approved`/`rejected`).
