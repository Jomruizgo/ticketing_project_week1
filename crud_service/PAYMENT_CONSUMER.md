# CRUD Service - Payment Events Consumer

Esta guía explica cómo el CRUD Service debe consumir y procesar los eventos de pago publicados por el Producer Service.

## Resumen

Cuando un usuario paga por un ticket, el Producer Service publica eventos de pago a RabbitMQ:

```
PaymentApprovedEvent (80%)
    ↓
CRUD Service debe:
  1. Actualizar ticket.status = "paid"
  2. Registrar el payment en la tabla payments
  3. Decrementar availableTickets del evento

PaymentRejectedEvent (20%)
    ↓
CRUD Service debe:
  1. Actualizar ticket.status = "released" (liberar el ticket)
  2. Registrar el pago fallido en auditoría
  3. Incrementar availableTickets del evento
  4. Mantener la reserva intacta para que otro usuario la tome
```

## Estructura de Datos

### Tabla: payments

Esta tabla ya existe en el schema.sql. Estructura:

```sql
CREATE TABLE payments (
    id SERIAL PRIMARY KEY,
    ticket_id INTEGER NOT NULL REFERENCES tickets(id),
    amount_cents INTEGER NOT NULL CHECK (amount_cents > 0),
    currency VARCHAR(3) DEFAULT 'USD',
    payment_status payment_status DEFAULT 'pending',
    paid_by VARCHAR(255),
    transaction_ref VARCHAR(255),
    paid_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Tabla: tickets

Campos relevantes:
```sql
- id: INTEGER (PK)
- status: ticket_status (available, reserved, paid, released, cancelled)
- reserved_at: TIMESTAMP
- expires_at: TIMESTAMP
- version: INTEGER (optimistic locking)
```

## Implementación en C#

### 1. Models (Crear si no existen)

```csharp
// Models/PaymentApprovedEvent.cs
public class PaymentApprovedEvent
{
    public int TicketId { get; set; }
    public int EventId { get; set; }
    public int AmountCents { get; set; }
    public string Currency { get; set; }
    public string PaymentBy { get; set; }
    public string TransactionRef { get; set; }
    public DateTime ApprovedAt { get; set; }
}

// Models/PaymentRejectedEvent.cs
public class PaymentRejectedEvent
{
    public int TicketId { get; set; }
    public int EventId { get; set; }
    public int AmountCents { get; set; }
    public string Currency { get; set; }
    public string PaymentBy { get; set; }
    public string RejectionReason { get; set; }
    public string? TransactionRef { get; set; }
    public DateTime RejectedAt { get; set; }
}
```

### 2. Consumer Service

```csharp
// Services/PaymentConsumerService.cs
public interface IPaymentConsumerService
{
    Task HandlePaymentApprovedAsync(PaymentApprovedEvent @event, CancellationToken cancellationToken);
    Task HandlePaymentRejectedAsync(PaymentRejectedEvent @event, CancellationToken cancellationToken);
}

public class PaymentConsumerService : IPaymentConsumerService
{
    private readonly ITicketService _ticketService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentConsumerService> _logger;

    public PaymentConsumerService(
        ITicketService ticketService,
        IUnitOfWork unitOfWork,
        ILogger<PaymentConsumerService> logger)
    {
        _ticketService = ticketService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandlePaymentApprovedAsync(PaymentApprovedEvent @event, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Obtener el ticket
            var ticket = await _ticketService.GetTicketByIdAsync(@event.TicketId, cancellationToken);
            
            if (ticket == null)
            {
                _logger.LogError("Ticket no encontrado: TicketId={TicketId}", @event.TicketId);
                throw new InvalidOperationException($"Ticket {ticket?.Id} not found");
            }

            // 2. Validar que está en estado "reserved"
            if (ticket.Status != TicketStatus.Reserved)
            {
                _logger.LogWarning(
                    "Ticket no está en estado reserved: TicketId={TicketId}, CurrentStatus={Status}",
                    @event.TicketId,
                    ticket.Status);
                throw new InvalidOperationException($"Ticket {ticket.Id} is not in reserved state");
            }

            // 3. Actualizar ticket a "paid"
            ticket.Status = TicketStatus.Paid;
            ticket.PaidAt = @event.ApprovedAt;
            ticket.UpdatedAt = DateTime.UtcNow;
            await _ticketService.UpdateTicketAsync(ticket, cancellationToken);

            // 4. Crear registro de pago
            var payment = new Payment
            {
                TicketId = @event.TicketId,
                AmountCents = @event.AmountCents,
                Currency = @event.Currency,
                PaymentStatus = PaymentStatus.Completed,
                PaidBy = @event.PaymentBy,
                TransactionRef = @event.TransactionRef,
                PaidAt = @event.ApprovedAt
            };
            
            await _unitOfWork.Payments.AddAsync(payment, cancellationToken);

            // 5. Confirmar transacción
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Pago aprobado procesado exitosamente: TicketId={TicketId}, EventId={EventId}, Amount={Amount}{Currency}",
                @event.TicketId,
                @event.EventId,
                @event.AmountCents / 100.0,
                @event.Currency);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            
            _logger.LogError(
                ex,
                "Error al procesar pago aprobado: TicketId={TicketId}, EventId={EventId}",
                @event.TicketId,
                @event.EventId);

            throw;
        }
    }

    public async Task HandlePaymentRejectedAsync(PaymentRejectedEvent @event, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Obtener el ticket
            var ticket = await _ticketService.GetTicketByIdAsync(@event.TicketId, cancellationToken);
            
            if (ticket == null)
            {
                _logger.LogError("Ticket no encontrado: TicketId={TicketId}", @event.TicketId);
                throw new InvalidOperationException($"Ticket {@event.TicketId} not found");
            }

            // 2. Validar que está en estado "reserved"
            if (ticket.Status != TicketStatus.Reserved)
            {
                _logger.LogWarning(
                    "Ticket no está en estado reserved: TicketId={TicketId}, CurrentStatus={Status}",
                    @event.TicketId,
                    ticket.Status);
                // Continuar igualmente para limpiar el pago
            }

            // 3. Liberar el ticket (cambiar a released)
            ticket.Status = TicketStatus.Released;
            ticket.ReservedAt = null;
            ticket.ExpiresAt = null;
            ticket.UpdatedAt = DateTime.UtcNow;
            await _ticketService.UpdateTicketAsync(ticket, cancellationToken);

            // 4. Crear registro de pago fallido
            var payment = new Payment
            {
                TicketId = @event.TicketId,
                AmountCents = @event.AmountCents,
                Currency = @event.Currency,
                PaymentStatus = PaymentStatus.Failed,
                PaidBy = @event.PaymentBy,
                TransactionRef = @event.TransactionRef
            };
            
            await _unitOfWork.Payments.AddAsync(payment, cancellationToken);

            // 5. Confirmar transacción
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogWarning(
                "Pago rechazado procesado: TicketId={TicketId}, EventId={EventId}, Reason={Reason}",
                @event.TicketId,
                @event.EventId,
                @event.RejectionReason);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            
            _logger.LogError(
                ex,
                "Error al procesar pago rechazado: TicketId={TicketId}, EventId={EventId}",
                @event.TicketId,
                @event.EventId);

            throw;
        }
    }
}
```

### 3. RabbitMQ Consumer (Hosted Service)

```csharp
// Services/PaymentEventConsumer.cs
public interface IPaymentEventConsumer
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public class RabbitMQPaymentEventConsumer : BackgroundService, IPaymentEventConsumer
{
    private readonly IConnection _connection;
    private readonly IPaymentConsumerService _paymentService;
    private readonly ILogger<RabbitMQPaymentEventConsumer> _logger;
    private IModel? _channel;

    private const string ExchangeName = "tickets";
    private const string PaymentApprovedQueueName = "payment-approved-queue";
    private const string PaymentRejectedQueueName = "payment-rejected-queue";
    private const string PaymentApprovedRoutingKey = "ticket.payments.approved";
    private const string PaymentRejectedRoutingKey = "ticket.payments.rejected";

    public RabbitMQPaymentEventConsumer(
        IConnection connection,
        IPaymentConsumerService paymentService,
        ILogger<RabbitMQPaymentEventConsumer> logger)
    {
        _connection = connection;
        _paymentService = paymentService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _channel = _connection.CreateModel();

            // Declarar exchange
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true);

            // Declarar y ligar queue de pagos aprobados
            _channel.QueueDeclare(
                queue: PaymentApprovedQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            _channel.QueueBind(
                queue: PaymentApprovedQueueName,
                exchange: ExchangeName,
                routingKey: PaymentApprovedRoutingKey);

            // Declarar y ligar queue de pagos rechazados
            _channel.QueueDeclare(
                queue: PaymentRejectedQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            _channel.QueueBind(
                queue: PaymentRejectedQueueName,
                exchange: ExchangeName,
                routingKey: PaymentRejectedRoutingKey);

            // Configurar consumers
            var approvedConsumer = new AsyncEventingBasicConsumer(_channel);
            approvedConsumer.Received += HandlePaymentApprovedAsync;

            var rejectedConsumer = new AsyncEventingBasicConsumer(_channel);
            rejectedConsumer.Received += HandlePaymentRejectedAsync;

            // Iniciar consumo
            _channel.BasicConsume(
                queue: PaymentApprovedQueueName,
                autoAck: false,
                consumerTag: "payment-approved-consumer",
                consumer: approvedConsumer);

            _channel.BasicConsume(
                queue: PaymentRejectedQueueName,
                autoAck: false,
                consumerTag: "payment-rejected-consumer",
                consumer: rejectedConsumer);

            _logger.LogInformation("Consumidor de eventos de pago iniciado");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Consumidor de eventos de pago detenido");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en consumidor de eventos de pago");
            throw;
        }
    }

    private async Task HandlePaymentApprovedAsync(object model, BasicDeliverEventArgs ea)
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var @event = JsonSerializer.Deserialize<PaymentApprovedEvent>(message);

            if (@event != null)
            {
                await _paymentService.HandlePaymentApprovedAsync(@event, CancellationToken.None);
                _channel?.BasicAck(ea.DeliveryTag, false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando pago aprobado");
            _channel?.BasicNack(ea.DeliveryTag, false, true); // Reintentar
        }
    }

    private async Task HandlePaymentRejectedAsync(object model, BasicDeliverEventArgs ea)
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var @event = JsonSerializer.Deserialize<PaymentRejectedEvent>(message);

            if (@event != null)
            {
                await _paymentService.HandlePaymentRejectedAsync(@event, CancellationToken.None);
                _channel?.BasicAck(ea.DeliveryTag, false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando pago rechazado");
            _channel?.BasicNack(ea.DeliveryTag, false, true); // Reintentar
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
```

### 4. Registrar en Program.cs

```csharp
// En Program.cs del CRUD Service

// Agregar servicios
builder.Services.AddScoped<IPaymentConsumerService, PaymentConsumerService>();
builder.Services.AddSingleton<IPaymentEventConsumer, RabbitMQPaymentEventConsumer>();

// Registrar como hosted service
builder.Services.AddHostedService(provider => provider.GetRequiredService<IPaymentEventConsumer>());
```

## Consideraciones Importantes

### 1. Idempotencia
Si el mismo evento se procesa dos veces (por reintentos de RabbitMQ):
- Usar `TransactionRef` como clave única en la tabla payments
- Verificar antes de insertar si ya existe el pago

### 2. Concurrencia
Usar optimistic locking con el campo `version` en tickets:
```csharp
ticket.Version++; // Incrementar automáticamente
```

### 3. Compensación
Si el procesamiento falla después de cambiar el status:
- RabbitMQ mantiene el mensaje en la queue
- El consumer lo reintenta automáticamente
- Transacciones de base de datos aseguran consistencia

### 4. Auditoría
Registrar todo en `ticket_history`:
```csharp
var history = new TicketHistory
{
    TicketId = ticket.Id,
    OldStatus = previousStatus,
    NewStatus = ticket.Status,
    Reason = $"Pago {(@event is PaymentApprovedEvent ? "aprobado" : "rechazado")}",
    ChangedAt = DateTime.UtcNow,
    ChangedBy = "PaymentConsumer"
};
await _unitOfWork.TicketHistories.AddAsync(history);
```

## Testing

### Con docker-compose
```bash
# Terminal 1: Ver logs
docker-compose logs -f crud-service

# Terminal 2: Enviar evento de pago (desde Producer)
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

# Terminal 3: Verificar cambios
curl http://localhost:8002/api/tickets/1
# Debería mostrar status: "paid" (80% del tiempo)
```

## Diagrama de Estados del Ticket

```
available
   │
   ├──reserve──► reserved
   │              │
   │              ├──payment approved──► paid ✓
   │              │
   │              └──payment rejected──► released
   │                                      │
   │◄─────────────────────────────────────┘
   │
   └──cancel──► cancelled
```

## Referencias

- [PAYMENT_SYSTEM.md](./PAYMENT_SYSTEM.md) - Documentación del sistema de pagos en Producer
- [PAYMENTS.md](./PAYMENTS.md) - API endpoints de pagos
- Database Schema: `scripts/schema.sql`
