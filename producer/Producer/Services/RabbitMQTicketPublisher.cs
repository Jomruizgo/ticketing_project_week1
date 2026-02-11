using System.Text.Json;
using Microsoft.Extensions.Options;
using Producer.Configurations;
using Producer.Models;
using RabbitMQ.Client;

namespace Producer.Services;

/// <summary>
/// Implementación del servicio para publicar eventos a RabbitMQ
/// </summary>
public class RabbitMQTicketPublisher : ITicketPublisher
{
    private readonly IConnection _connection;
    private readonly RabbitMQOptions _options;
    private readonly ILogger<RabbitMQTicketPublisher> _logger;

    public RabbitMQTicketPublisher(
        IConnection connection,
        IOptions<RabbitMQOptions> options,
        ILogger<RabbitMQTicketPublisher> logger)
    {
        _connection = connection;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Publica un evento de ticket reservado a RabbitMQ
    /// </summary>
    public async Task PublishTicketReservedAsync(
        TicketReservedEvent ticketEvent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var channel = _connection.CreateModel();

            // Serializar el evento a JSON
            var json = JsonSerializer.Serialize(ticketEvent);
            var body = System.Text.Encoding.UTF8.GetBytes(json);

            // Crear propiedades básicas
            var properties = channel.CreateBasicProperties();
            properties.ContentType = "application/json";
            properties.DeliveryMode = 2; // Persistente
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            // Publicar el evento al exchange
            channel.BasicPublish(
                exchange: _options.ExchangeName,
                routingKey: _options.TicketReservedRoutingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "Evento de ticket reservado publicado. TicketId: {TicketId}, OrderId: {OrderId}",
                ticketEvent.TicketId,
                ticketEvent.OrderId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al publicar evento de ticket reservado. TicketId: {TicketId}",
                ticketEvent.TicketId);
            throw;
        }
    }
}
