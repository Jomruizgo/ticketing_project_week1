using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MsPaymentService.Worker.Configurations;
using MsPaymentService.Worker.Messaging.RabbitMQ;
using MsPaymentService.Worker.Models.DTOs;
using MsPaymentService.Worker.Models.Events;

namespace MsPaymentService.Worker.Handlers;

/// <summary>
/// Recibe la solicitud de pago, decide si se aprueba o rechaza,
/// y publica el evento correspondiente a RabbitMQ.
/// La decision que antes tomaba el Producer ahora vive aqui.
/// </summary>
public class PaymentRequestedEventHandler : IPaymentEventHandler
{
    private readonly RabbitMQConnection _connection;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<PaymentRequestedEventHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PaymentRequestedEventHandler(
        RabbitMQConnection connection,
        IOptions<RabbitMQSettings> settings,
        ILogger<PaymentRequestedEventHandler> logger)
    {
        _connection = connection;
        _settings = settings.Value;
        _logger = logger;
    }

    public string QueueName => _settings.RequestedQueueName;

    public async Task<ValidationResult> HandleAsync(string json, CancellationToken cancellationToken = default)
    {
        var evt = JsonSerializer.Deserialize<PaymentRequestedEvent>(json, JsonOptions);
        if (evt == null)
            return ValidationResult.Failure("Invalid JSON for PaymentRequestedEvent");

        _logger.LogInformation(
            "Processing payment request. TicketId={TicketId}, Amount={Amount}{Currency}",
            evt.TicketId, evt.AmountCents / 100.0, evt.Currency);

        // Simular procesamiento de pago (80% aprobacion)
        // En produccion: llamada al gateway real (Stripe, PayPal, etc.)
        await Task.Delay(Random.Shared.Next(100, 500), cancellationToken);
        var isApproved = Random.Shared.Next(0, 100) < 80;

        if (isApproved)
            PublishApproved(evt);
        else
            PublishRejected(evt);

        return ValidationResult.Success();
    }

    private void PublishApproved(PaymentRequestedEvent request)
    {
        var approvedEvent = new PaymentApprovedEvent
        {
            TicketId = request.TicketId,
            EventId = request.EventId,
            AmountCents = request.AmountCents,
            Currency = request.Currency,
            PaymentBy = request.PaymentBy,
            TransactionRef = request.TransactionRef ?? $"TXN-{Guid.NewGuid()}",
            ApprovedAt = DateTime.UtcNow
        };

        Publish("ticket.payments.approved", approvedEvent);

        _logger.LogInformation(
            "Payment approved. TicketId={TicketId}, Ref={Ref}",
            request.TicketId, approvedEvent.TransactionRef);
    }

    private void PublishRejected(PaymentRequestedEvent request)
    {
        var rejectedEvent = new PaymentRejectedEvent
        {
            TicketId = request.TicketId,
            EventId = request.EventId,
            RejectionReason = "Fondos insuficientes o tarjeta rechazada",
            RejectedAt = DateTime.UtcNow,
            EventTimestamp = DateTime.UtcNow
        };

        Publish("ticket.payments.rejected", rejectedEvent);

        _logger.LogWarning(
            "Payment rejected. TicketId={TicketId}, Reason={Reason}",
            request.TicketId, rejectedEvent.RejectionReason);
    }

    private void Publish<T>(string routingKey, T payload)
    {
        var channel = _connection.GetChannel();
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        channel.BasicPublish(
            exchange: _settings.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }
}
