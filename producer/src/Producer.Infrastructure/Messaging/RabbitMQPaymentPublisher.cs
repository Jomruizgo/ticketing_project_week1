using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Producer.Domain.Events;
using Producer.Domain.Ports;
using RabbitMQ.Client;

namespace Producer.Infrastructure.Messaging;

public class RabbitMQPaymentPublisher : IPaymentEventPublisher
{
    private readonly IConnection _connection;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQPaymentPublisher> _logger;

    public RabbitMQPaymentPublisher(
        IConnection connection,
        IOptions<RabbitMQSettings> settings,
        ILogger<RabbitMQPaymentPublisher> logger)
    {
        _connection = connection;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task PublishAsync(PaymentRequestedEvent evt, CancellationToken ct = default)
    {
        using var channel = _connection.CreateModel();

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        channel.BasicPublish(
            exchange: _settings.ExchangeName,
            routingKey: _settings.PaymentRequestedRoutingKey,
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "Payment request published. TicketId={TicketId}, EventId={EventId}",
            evt.TicketId,
            evt.EventId);

        await Task.CompletedTask;
    }
}
