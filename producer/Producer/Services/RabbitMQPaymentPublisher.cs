using System.Text;
using System.Text.Json;
using Producer.Models;
using RabbitMQ.Client;

namespace Producer.Services;

/// <summary>
/// Implementación del publicador de eventos de pago usando RabbitMQ
/// </summary>
public class RabbitMQPaymentPublisher : IPaymentPublisher
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMQPaymentPublisher> _logger;
    private const string ExchangeName = "tickets";
    private const string PaymentApprovedRoutingKey = "ticket.payments.approved";
    private const string PaymentRejectedRoutingKey = "ticket.payments.rejected";

    public RabbitMQPaymentPublisher(
        IConnection connection,
        ILogger<RabbitMQPaymentPublisher> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publica un evento de pago aprobado a RabbitMQ
    /// </summary>
    public async Task PublishPaymentApprovedAsync(PaymentApprovedEvent paymentEvent, CancellationToken cancellationToken = default)
    {
        if (paymentEvent == null)
        {
            throw new ArgumentNullException(nameof(paymentEvent));
        }

        try
        {
            using var channel = _connection.CreateModel();

            // Declarar exchange (topic)
            channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Serializar evento
            var message = JsonSerializer.Serialize(paymentEvent);
            var body = Encoding.UTF8.GetBytes(message);

            // Propiedades del mensaje
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            // Publicar
            channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: PaymentApprovedRoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "Evento de pago aprobado publicado: TicketId={TicketId}, EventId={EventId}, Amount={Amount}{Currency}, TransactionRef={TransactionRef}",
                paymentEvent.TicketId,
                paymentEvent.EventId,
                paymentEvent.AmountCents / 100.0,
                paymentEvent.Currency,
                paymentEvent.TransactionRef);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al publicar evento de pago aprobado: TicketId={TicketId}, EventId={EventId}",
                paymentEvent.TicketId,
                paymentEvent.EventId);

            throw;
        }
    }

    /// <summary>
    /// Publica un evento de pago rechazado a RabbitMQ
    /// </summary>
    public async Task PublishPaymentRejectedAsync(PaymentRejectedEvent paymentEvent, CancellationToken cancellationToken = default)
    {
        if (paymentEvent == null)
        {
            throw new ArgumentNullException(nameof(paymentEvent));
        }

        try
        {
            using var channel = _connection.CreateModel();

            // Declarar exchange (topic)
            channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Serializar evento
            var message = JsonSerializer.Serialize(paymentEvent);
            var body = Encoding.UTF8.GetBytes(message);

            // Propiedades del mensaje
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            // Publicar
            channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: PaymentRejectedRoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogWarning(
                "Evento de pago rechazado publicado: TicketId={TicketId}, EventId={EventId}, Reason={Reason}",
                paymentEvent.TicketId,
                paymentEvent.EventId,
                paymentEvent.RejectionReason);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al publicar evento de pago rechazado: TicketId={TicketId}, EventId={EventId}",
                paymentEvent.TicketId,
                paymentEvent.EventId);

            throw;
        }
    }
}
