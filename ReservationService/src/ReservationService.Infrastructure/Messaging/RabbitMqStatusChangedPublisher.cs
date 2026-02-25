using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ReservationService.Application.Interfaces;

namespace ReservationService.Infrastructure.Messaging;

public class RabbitMqStatusChangedPublisher : IStatusChangedPublisher
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMqStatusChangedPublisher> _logger;

    public RabbitMqStatusChangedPublisher(
        IOptions<RabbitMQSettings> settings,
        ILogger<RabbitMqStatusChangedPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task PublishAsync(long ticketId, string newStatus, CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.Username,
            Password = _settings.Password
        };

        var connection = await factory.CreateConnectionAsync(cancellationToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        var payload = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new
        {
            ticketId,
            newStatus,
            changedAt = DateTime.UtcNow
        });

        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        await channel.BasicPublishAsync(
            exchange: _settings.ExchangeName,
            routingKey: _settings.StatusChangedRoutingKey,
            mandatory: false,
            basicProperties: props,
            body: payload,
            cancellationToken: cancellationToken);

        await channel.CloseAsync(cancellationToken);
        await connection.CloseAsync(cancellationToken);

        _logger.LogInformation(
            "Published status change for ticket {TicketId} to {Status}",
            ticketId,
            newStatus);
    }
}
