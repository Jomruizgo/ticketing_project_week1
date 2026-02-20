using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Producer.Domain.Events;
using Producer.Domain.Ports;
using RabbitMQ.Client;

namespace Producer.Infrastructure.Messaging;

public class RabbitMQTicketPublisher : ITicketEventPublisher
{
    private readonly IConnection _connection;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQTicketPublisher> _logger;

    public RabbitMQTicketPublisher(
        IConnection connection,
        IOptions<RabbitMQSettings> settings,
        ILogger<RabbitMQTicketPublisher> logger)
    {
        _connection = connection;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task PublishAsync(TicketReservedEvent evt, CancellationToken ct = default)
    {
        try
        {
            using var channel = _connection.CreateModel();

            var json = JsonSerializer.Serialize(evt);
            var body = System.Text.Encoding.UTF8.GetBytes(json);

            var properties = channel.CreateBasicProperties();
            properties.ContentType = "application/json";
            properties.DeliveryMode = 2;
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            channel.BasicPublish(
                exchange: _settings.ExchangeName,
                routingKey: _settings.TicketReservedRoutingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "Evento de ticket reservado publicado. TicketId: {TicketId}, OrderId: {OrderId}",
                evt.TicketId,
                evt.OrderId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al publicar evento de ticket reservado. TicketId: {TicketId}",
                evt.TicketId);
            throw;
        }
    }
}
