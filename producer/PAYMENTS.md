# Payment Processing Flow - Producer Service

## Overview

El Producer Service ahora soporta procesamiento de pagos a través de un nuevo endpoint `/api/payments/process`. Este endpoint implementa un flujo asincrónico que publica eventos de pago aprobado o rechazado a RabbitMQ.

## Architecture Pattern

```
┌─────────────┐
│   Frontend  │
│  (Browser)  │
└──────┬──────┘
       │ POST /api/payments/process
       │ ProcessPaymentRequest
       ▼
┌──────────────────────┐
│  Producer Service    │
│  Port: 8001          │
└──────┬───────────────┘
       │
       ├─► Simula procesamiento de pago (80% éxito)
       │
       ├─► If Approved:
       │   └─► Publica a RabbitMQ
       │       Exchange: "tickets"
       │       Routing Key: "ticket.payments.approved"
       │       Event: PaymentApprovedEvent
       │
       └─► If Rejected:
           └─► Publica a RabbitMQ
               Exchange: "tickets"
               Routing Key: "ticket.payments.rejected"
               Event: PaymentRejectedEvent
       
       │
       ▼
   RabbitMQ (Message Broker)
       │
       ├─► payment-approved-queue (suscriptores)
       │   └─► Ej: Payment Service, Notification Service
       │
       └─► payment-rejected-queue (suscriptores)
           └─► Ej: Refund Service, Notification Service
```

## Endpoints

### Process Payment

**POST** `/api/payments/process`

Procesa un pago y publica el evento correspondiente a RabbitMQ.

#### Request Body

```json
{
  "ticketId": 1,
  "eventId": 1,
  "amountCents": 5000,
  "currency": "USD",
  "paymentBy": "usuario@ejemplo.com",
  "paymentMethodId": "card_1234",
  "transactionRef": "txn_external_123"
}
```

#### Parameters

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `ticketId` | int | ✓ | ID del ticket para el cual se procesa el pago |
| `eventId` | int | ✓ | ID del evento asociado |
| `amountCents` | int | ✓ | Monto en centavos (5000 = $50.00) |
| `currency` | string | | Moneda (default: "USD") |
| `paymentBy` | string | ✓ | Email del pagador |
| `paymentMethodId` | string | ✓ | ID del método de pago (tarjeta, wallet, etc) |
| `transactionRef` | string | | Referencia de transacción externa |

#### Response - 202 Accepted

```json
{
  "message": "Pago encolado para procesamiento",
  "ticketId": 1,
  "eventId": 1,
  "status": "approved"
}
```

#### Status Codes

- **202 Accepted** - El pago ha sido encolado para procesamiento (éxito o error en validación)
- **400 Bad Request** - Parámetros inválidos
- **500 Internal Server Error** - Error al publicar a RabbitMQ

## Events Published

### PaymentApprovedEvent

Se publica cuando el pago es aprobado.

**Routing Key:** `ticket.payments.approved`

```json
{
  "ticketId": 1,
  "eventId": 1,
  "amountCents": 5000,
  "currency": "USD",
  "paymentBy": "usuario@ejemplo.com",
  "transactionRef": "TXN-abc123",
  "approvedAt": "2026-02-10T15:30:45.123Z"
}
```

### PaymentRejectedEvent

Se publica cuando el pago es rechazado.

**Routing Key:** `ticket.payments.rejected`

```json
{
  "ticketId": 1,
  "eventId": 1,
  "amountCents": 5000,
  "currency": "USD",
  "paymentBy": "usuario@ejemplo.com",
  "rejectionReason": "Fondos insuficientes o tarjeta rechazada",
  "transactionRef": "TXN-abc123",
  "rejectedAt": "2026-02-10T15:30:45.123Z"
}
```

## Integration with Other Services

### Expected Consumers

Los siguientes servicios pueden suscribirse a estos eventos:

1. **CRUD Service** - Actualiza el estado del ticket a "paid" (si es aprobado)
2. **Payment Service** - Registra el pago en la base de datos
3. **Notification Service** - Envía confirmación de pago o notificación de rechazo
4. **Refund Service** - Procesa reembolsos para pagos rechazados
5. **Analytics Service** - Registra métricas de pagos

## Simulation

El endpoint incluye una simulación de procesamiento de pago:
- **80% de probabilidad** de que el pago sea aprobado
- **20% de probabilidad** de que el pago sea rechazado
- Latencia artificial: 100-500ms

En producción, esto se reemplazaría con una integración real de gateway de pago (Stripe, PayPal, etc).

## Testing

### Test con curl

**Pago Aprobado (Esperado):**

```bash
curl -X POST http://localhost:8001/api/payments/process \
  -H "Content-Type: application/json" \
  -d '{
    "ticketId": 1,
    "eventId": 1,
    "amountCents": 5000,
    "currency": "USD",
    "paymentBy": "usuario@ejemplo.com",
    "paymentMethodId": "card_1234",
    "transactionRef": "txn_test_123"
  }'
```

**Respuesta esperada:**

```json
HTTP/1.1 202 Accepted

{
  "message": "Pago encolado para procesamiento",
  "ticketId": 1,
  "eventId": 1,
  "status": "approved"
}
```

### Verificar en RabbitMQ

Acceder a la UI de RabbitMQ en `http://localhost:15672`:

1. **Queues** → Buscar colas que consuman `ticket.payments.approved` o `ticket.payments.rejected`
2. **Messages** → Ver los eventos publicados en cada cola
3. **Bindings** → Verificar que las colas estén ligadas al exchange `tickets`

## Architecture Benefits

1. **Decoupling** - El Producer no necesita conocer a los consumers
2. **Scalability** - Múltiples consumers pueden procesar pagos en paralelo
3. **Resilience** - Si un consumer falla, el mensaje persiste en RabbitMQ
4. **Async Processing** - El frontend recibe 202 inmediatamente
5. **Traceability** - Cada evento tiene transactionRef para auditoría

## Future Enhancements

- [ ] Integración con gateway de pago real (Stripe, PayPal)
- [ ] Validación de tarjeta en tiempo real
- [ ] Manejo de reintentos automáticos
- [ ] Circuit breaker para fallos de gateway
- [ ] Webhook callbacks desde gateway de pago
- [ ] Soporte para múltiples monedas
- [ ] Logging centralizado de transacciones
