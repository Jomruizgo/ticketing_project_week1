using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CrudService.Infrastructure.Messaging;

/// <summary>
/// BackgroundService que consume q.ticket.status.changed y notifica al TicketStatusHub.
/// Es un adaptador de entrada: traduce mensajes RabbitMQ en notificaciones SSE.
/// </summary>
public class TicketStatusConsumer : BackgroundService
{
    private readonly ITicketStatusNotifier _notifier;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<TicketStatusConsumer> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// DIP: depende de la abstracción ITicketStatusNotifier, no de TicketStatusHub concreto.
    /// </summary>
    public TicketStatusConsumer(
        ITicketStatusNotifier notifier,
        IOptions<RabbitMQSettings> settings,
        ILogger<TicketStatusConsumer> logger)
    {
        _notifier = notifier;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ConnectAndConsume(stoppingToken);
        return Task.CompletedTask;
    }

    private void ConnectAndConsume(CancellationToken stoppingToken)
    {
        const int maxRetries = 24;
        const int retrySeconds = 5;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            if (stoppingToken.IsCancellationRequested) return;

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.Host,
                    Port = _settings.Port,
                    UserName = _settings.Username,
                    Password = _settings.Password,
                    AutomaticRecoveryEnabled = true,
                    DispatchConsumersAsync = true
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _channel.BasicQos(0, 10, false);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += OnMessageAsync;

                _channel.BasicConsume(
                    queue: _settings.StatusChangedQueueName,
                    autoAck: false,
                    consumer: consumer);

                _logger.LogInformation("TicketStatusConsumer listening on {Queue}", _settings.StatusChangedQueueName);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "RabbitMQ not ready (attempt {Attempt}/{Max}). Retrying in {Seconds}s...",
                    attempt, maxRetries, retrySeconds);
                Task.Delay(TimeSpan.FromSeconds(retrySeconds), stoppingToken).Wait(stoppingToken);
            }
        }

        _logger.LogError("TicketStatusConsumer could not connect to RabbitMQ after {Max} attempts.", maxRetries);
    }

    private async Task OnMessageAsync(object sender, BasicDeliverEventArgs args)
    {
        try
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            ProcessMessage(json);

            _channel?.BasicAck(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ticket.status.changed");
            _channel?.BasicNack(args.DeliveryTag, false, requeue: false);
        }

        await Task.CompletedTask;
    }

    internal void ProcessMessage(string json)
    {
        var update = StatusPayloadParser.TryParse(json);
        if (update is null)
        {
            _logger.LogWarning("ticket.status.changed payload inválido o nulo: {Payload}",
                json?.Length > 200 ? json[..200] : json);
            return;
        }

        if (string.IsNullOrWhiteSpace(update.NewStatus))
        {
            _logger.LogWarning(
                "ticket.status.changed ignorado por estado vacío. TicketId={TicketId}",
                update.TicketId);
            return;
        }

        _logger.LogInformation(
            "ticket.status.changed received. TicketId={TicketId}, NewStatus={Status}",
            update.TicketId, update.NewStatus);

        _notifier.Notify(update.TicketId, update.NewStatus);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}
