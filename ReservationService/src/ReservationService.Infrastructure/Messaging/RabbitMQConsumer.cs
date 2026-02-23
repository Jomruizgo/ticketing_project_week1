using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReservationService.Application.DTOs.ProcessReservation;
using ReservationService.Application.Interfaces;

namespace ReservationService.Infrastructure.Messaging;

public class RabbitMQConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;
    private IChannel? _publishChannel;

    public RabbitMQConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMQSettings> settings,
        ILogger<RabbitMQConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.Username,
            Password = _settings.Password
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
        _publishChannel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        _logger.LogInformation("Connected to RabbitMQ. Listening on queue: {Queue}", _settings.QueueName);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            _logger.LogInformation("Message received: {Json}", json);

            try
            {
                var message = JsonSerializer.Deserialize<ProcessReservationCommand>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (message is not null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    // HUMAN CHECK:
                    // El adapter de infraestructura debe invocar el caso de uso por su
                    // puerto de entrada para evitar acoplamiento al handler concreto.
                    var useCase = scope.ServiceProvider.GetRequiredService<IProcessReservationUseCase>();
                    await useCase.HandleAsync(message, stoppingToken);

                    // HUMAN CHECK:
                    // El evento status.changed debe publicarse solo después de procesar
                    // la reserva para evitar notificar al cliente un estado que aún no fue
                    // persistido. Mantener este orden reduce race conditions con SSE.
                    await PublishStatusChangedAsync(message.TicketId, "reserved", stoppingToken);
                }

                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Json}", json);
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(_settings.QueueName, autoAck: false, consumer, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task PublishStatusChangedAsync(long ticketId, string newStatus, CancellationToken cancellationToken)
    {
        if (_publishChannel is null) return;

        var payload = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new
        {
            TicketId = ticketId,
            NewStatus = newStatus,
            ChangedAt = DateTime.UtcNow
        });

        var props = new RabbitMQ.Client.BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        await _publishChannel.BasicPublishAsync(
            exchange: _settings.ExchangeName,
            routingKey: _settings.StatusChangedRoutingKey,
            mandatory: false,
            basicProperties: props,
            body: payload,
            cancellationToken: cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping consumer...");

        if (_publishChannel is not null) await _publishChannel.CloseAsync(cancellationToken);
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }
}
