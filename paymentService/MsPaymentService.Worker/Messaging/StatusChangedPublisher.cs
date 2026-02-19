using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MsPaymentService.Worker.Configurations;
using MsPaymentService.Worker.Models.Events;
using MsPaymentService.Worker.Messaging.RabbitMQ;

namespace MsPaymentService.Worker.Messaging;

public interface IStatusChangedPublisher
{
    void Publish(int ticketId, string newStatus);
}

public class StatusChangedPublisher : IStatusChangedPublisher
{
    private readonly RabbitMQConnection _connection;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<StatusChangedPublisher> _logger;

    private const string RoutingKey = "ticket.status.changed";

    public StatusChangedPublisher(
        RabbitMQConnection connection,
        IOptions<RabbitMQSettings> settings,
        ILogger<StatusChangedPublisher> logger)
    {
        _connection = connection;
        _settings = settings.Value;
        _logger = logger;
    }

    public void Publish(int ticketId, string newStatus)
    {
        var channel = _connection.GetChannel();

        var evt = new TicketStatusChangedEvent
        {
            TicketId = ticketId,
            NewStatus = newStatus,
            ChangedAt = DateTime.UtcNow
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        channel.BasicPublish(
            exchange: _settings.ExchangeName,
            routingKey: RoutingKey,
            mandatory: false,
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "ticket.status.changed published. TicketId={TicketId}, NewStatus={NewStatus}",
            ticketId, newStatus);
    }
}
