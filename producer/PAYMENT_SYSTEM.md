# Sistema de Pagos Distribuido - Ticketing System

## Overview

El nuevo sistema de pagos implementa un flujo completamente asincrónico y distribuido usando RabbitMQ. Cuando un usuario intenta pagar por un ticket, el Producer Service publica dos tipos de eventos dependiendo del resultado.

## Flujo Completo del Sistema

```
USER (Frontend)
   │
   ├─ POST /api/tickets/reserve (Reserva ticket)
   │  └─► Producer Service
   │      └─► RabbitMQ: ticket.reserved
   │          └─► CRUD Service: Actualiza estado a "reserved"
   │
   └─ POST /api/payments/process (Paga por ticket)
      └─► Producer Service
          ├─ 80% → PaymentApprovedEvent
          │   └─► RabbitMQ: ticket.payments.approved
          │       ├─► CRUD Service: Actualiza a "paid"
          │       ├─► Payment Service: Registra pago
          │       └─► Notification Service: Envía confirmación
          │
          └─ 20% → PaymentRejectedEvent
              └─► RabbitMQ: ticket.payments.rejected
                  ├─► CRUD Service: Mantiene en "reserved"
                  ├─► Refund Service: Libera ticket
                  └─► Notification Service: Envía error
```

## Componentes Nuevos

### 1. Models

#### `ProcessPaymentRequest`
```csharp
public class ProcessPaymentRequest
{
    public int TicketId { get; set; }
    public int EventId { get; set; }
    public int AmountCents { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentBy { get; set; }
    public string PaymentMethodId { get; set; }
    public string? TransactionRef { get; set; }
}
```

#### `PaymentApprovedEvent`
```csharp
public class PaymentApprovedEvent
{
    public int TicketId { get; set; }
    public int EventId { get; set; }
    public int AmountCents { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentBy { get; set; }
    public string TransactionRef { get; set; }
    public DateTime ApprovedAt { get; set; }
}
```

#### `PaymentRejectedEvent`
```csharp
public class PaymentRejectedEvent
{
    public int TicketId { get; set; }
    public int EventId { get; set; }
    public int AmountCents { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentBy { get; set; }
    public string RejectionReason { get; set; }
    public string? TransactionRef { get; set; }
    public DateTime RejectedAt { get; set; }
}
```

### 2. Services

#### `IPaymentPublisher`
Interfaz para publicar eventos de pago a RabbitMQ

```csharp
public interface IPaymentPublisher
{
    Task PublishPaymentApprovedAsync(PaymentApprovedEvent paymentEvent, CancellationToken cancellationToken = default);
    Task PublishPaymentRejectedAsync(PaymentRejectedEvent paymentEvent, CancellationToken cancellationToken = default);
}
```

#### `RabbitMQPaymentPublisher`
Implementación que:
- Publica a exchange `tickets` (topic)
- Usa routing key `ticket.payments.approved` para aprobados
- Usa routing key `ticket.payments.rejected` para rechazados
- Implementa logging detallado
- Manejo de errores robusta

### 3. Controllers

#### `PaymentsController`
Nuevo endpoint: `POST /api/payments/process`

Características:
- Validación exhaustiva de entrada
- Simulación de procesamiento (80% éxito, 20% fallo)
- Publicación automática de eventos
- Respuesta 202 Accepted (asincrónica)
- Logging detallado

## RabbitMQ Configuration

### Exchange
- **Name:** `tickets`
- **Type:** Topic
- **Durable:** Yes

### Routing Keys
- `ticket.payments.approved` - Para pagos exitosos
- `ticket.payments.rejected` - Para pagos fallidos

### Expected Queues (a implementar por consumers)

**payment-approved-queue**
```
- Binding: tickets / ticket.payments.approved
- Consumers: 
  - CRUD Service (actualizar ticket a paid)
  - Payment Service (registrar en DB)
  - Notification Service (enviar email)
```

**payment-rejected-queue**
```
- Binding: tickets / ticket.payments.rejected
- Consumers:
  - CRUD Service (liberar ticket)
  - Refund Service (procesar reembolso)
  - Notification Service (notificar al usuario)
```

## Diferencias Clave vs Reservas

| Aspecto | Reserva | Pago |
|--------|---------|------|
| **Routing Key** | `ticket.reserved` | `ticket.payments.approved/rejected` |
| **Resultado** | Un evento único | Dos eventos posibles |
| **Validación** | Basada en disponibilidad | Basada en fondos (simulado) |
| **Estado Final** | reserved | paid / reserved (mantenido) |
| **Reintentos** | Implícito en polling | A cargo del consumer |

## Testing

### 1. Terminal 1: Ver logs del Producer
```bash
dotnet run --project producer/src/Producer.Api/Producer.Api.csproj
```

### 2. Terminal 2: Hacer llamadas HTTP
```bash
# Reservar un ticket
curl -X POST http://localhost:8001/api/tickets/reserve \
  -H "Content-Type: application/json" \
  -d '{
    "eventId": 1,
    "ticketId": 1,
    "orderId": "ORD-001",
    "reservedBy": "test@test.com",
    "expiresInSeconds": 600
  }'

# Procesar pago
curl -X POST http://localhost:8001/api/payments/process \
  -H "Content-Type: application/json" \
  -d '{
    "ticketId": 1,
    "eventId": 1,
    "amountCents": 5000,
    "currency": "USD",
    "paymentBy": "test@test.com",
    "paymentMethodId": "card_1234"
  }'
```

### 3. Terminal 3: Monitor RabbitMQ
Acceder a `http://localhost:15672` (user: guest, password: guest)
- Queues → Ver `ticket.payments.approved` y `ticket.payments.rejected`
- Messages → Inspeccionar payloads

## Diagrama de Secuencia

```
Frontend                Producer             RabbitMQ          CRUD Service
   │                       │                    │                    │
   │──POST /payments/process──►               │                    │
   │                       │                    │                    │
   │                    [Simular pago]        │                    │
   │                       │                    │                    │
   │◄──────202 Accepted────│                    │                    │
   │                       │                    │                    │
   │                       ├─PaymentApprovedEvent──►                │
   │                       │  (PublishAsync)        │                │
   │                       │                    ┌──Consume──────────►│
   │                       │                    │   Update: paid     │
   │                       │                    │                    │
   │ [Polling /tickets/{id}]                   │                    │
   │─────────────────────────────────────────────────────────────────►
   │                       │                    │  status: "paid"    │
   │◄──────status: paid─────────────────────────────────────────────│
   │                       │                    │                    │
```

## Integración con Frontend

El frontend debe:

1. **Después de reserva aprobada:**
   - Mostrar formulario de pago
   - Permitir entrada de datos de tarjeta

2. **Al hacer POST /api/payments/process:**
   - Mostrar "Procesando pago..."
   - NO asumir éxito inmediatamente

3. **Después de recibir 202:**
   - Empezar a hacer polling a `/api/tickets/{id}`
   - Esperar hasta que status cambie de "reserved" a "paid"
   - Mostrar confirmación cuando status = "paid"
   - Mostrar error si timeout o status = "released"

## Consideraciones de Producción

- ⚠️ La simulación de pago (80% éxito) debe reemplazarse con gateway real
- ⚠️ Los montos deben validarse contra precios en CRUD Service
- ⚠️ Implementar idempotencia (mismo PaymentMethodId no procesa dos veces)
- ⚠️ Agregar rate limiting para prevenir abuse
- ⚠️ Auditoría completa de transacciones
- ⚠️ Soporte para múltiples métodos de pago
- ⚠️ Reembolsos automáticos si ticket expira sin completar pago

## Referencias

- [PAYMENTS.md](./PAYMENTS.md) - Documentación técnica de endpoints
- [ARCHITECTURE.md](./ARCHITECTURE.md) - Arquitectura general del Producer
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
