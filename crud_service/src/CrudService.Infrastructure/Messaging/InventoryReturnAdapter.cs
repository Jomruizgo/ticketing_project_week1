using System.Text;
using System.Text.Json;
using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CrudService.Infrastructure.Messaging;

public class InventoryReturnAdapter : IInventoryReturnPort
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<InventoryReturnAdapter> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public InventoryReturnAdapter(
        IOptions<RabbitMQSettings> settings,
        ILogger<InventoryReturnAdapter> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task ReturnToInventoryAsync(long ticketId, long eventId)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.Username,
            Password = _settings.Password
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        var payload = new
        {
            ticketId,
            eventId,
            returnedAt = DateTime.UtcNow
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOptions));
        channel.BasicPublish(
            exchange: "tickets",
            routingKey: "ticket.returned_to_inventory",
            basicProperties: null,
            body: body);

        _logger.LogInformation(
            "Published ticket.returned_to_inventory for TicketId={TicketId}, EventId={EventId}",
            ticketId, eventId);

        await Task.CompletedTask;
    }
}
