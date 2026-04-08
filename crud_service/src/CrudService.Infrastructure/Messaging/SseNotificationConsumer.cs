using System.Text;
using System.Text.Json;
using CrudService.Infrastructure.Sse;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CrudService.Infrastructure.Messaging;

public class SseNotificationConsumer : BackgroundService
{
    private readonly IWaitlistSseNotifier _notifier;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<SseNotificationConsumer> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SseNotificationConsumer(
        IWaitlistSseNotifier notifier,
        IOptions<RabbitMQSettings> settings,
        ILogger<SseNotificationConsumer> logger)
    {
        _notifier = notifier;
        _settings = settings?.Value ?? new RabbitMQSettings();
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

                var queueName = $"q.waitlist.sse.{Guid.NewGuid()}";
                _channel.QueueDeclare(queue: queueName, durable: false, exclusive: true, autoDelete: true);
                _channel.QueueBind(queue: queueName, exchange: "tickets", routingKey: "waitlist.opportunity.activated");
                _channel.QueueBind(queue: queueName, exchange: "tickets", routingKey: "waitlist.opportunity.expired");

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += OnMessageAsync;

                _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

                _logger.LogInformation("SseNotificationConsumer listening on {Queue}", queueName);
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

        _logger.LogError("SseNotificationConsumer could not connect to RabbitMQ after {Max} attempts.", maxRetries);
    }

    private async Task OnMessageAsync(object sender, BasicDeliverEventArgs args)
    {
        try
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            var routingKey = args.RoutingKey;

            ProcessMessage(json, routingKey);

            _channel?.BasicAck(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SSE notification message");
            _channel?.BasicAck(args.DeliveryTag, false);
        }

        await Task.CompletedTask;
    }

    internal void ProcessMessage(string json, string routingKey)
    {
        try
        {
            if (routingKey == "waitlist.opportunity.activated")
                ProcessActivatedMessage(json);
            else if (routingKey == "waitlist.opportunity.expired")
                ProcessExpiredMessage(json);
            else
                _logger.LogWarning("Unknown routing key: {RoutingKey}", routingKey);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Malformed JSON in SSE notification: {Payload}",
                json?.Length > 200 ? json[..200] : json);
        }
    }

    private void ProcessActivatedMessage(string json)
    {
        var message = JsonSerializer.Deserialize<OpportunityActivatedEvent>(json, JsonOptions);
        if (message is null || string.IsNullOrWhiteSpace(message.BuyerEmail))
        {
            _logger.LogWarning("Invalid opportunity activated payload: {Payload}", json);
            return;
        }

        var remainingMinutes = (int)Math.Floor((message.ExpiresAt - DateTime.UtcNow).TotalMinutes);

        var ssePayload = JsonSerializer.Serialize(new
        {
            opportunityId = message.OpportunityId,
            ticketId = message.TicketId,
            eventId = message.EventId,
            expiresAt = message.ExpiresAt.ToString("O"),
            remainingMinutes
        });

        _notifier.SendEventAsync(message.BuyerEmail, "opportunity_activated", ssePayload);

        _logger.LogInformation(
            "SSE opportunity_activated dispatched for {Email}, OpportunityId={OpportunityId}",
            message.BuyerEmail, message.OpportunityId);
    }

    private void ProcessExpiredMessage(string json)
    {
        var message = JsonSerializer.Deserialize<OpportunityActivatedEvent>(json, JsonOptions);
        if (message is null || string.IsNullOrWhiteSpace(message.BuyerEmail))
        {
            _logger.LogWarning("Invalid opportunity expired payload: {Payload}", json);
            return;
        }

        var ssePayload = JsonSerializer.Serialize(new
        {
            opportunityId = message.OpportunityId,
            eventId = message.EventId,
            reason = "timeout"
        });

        _notifier.SendEventAsync(message.BuyerEmail, "opportunity_expired", ssePayload);

        _logger.LogInformation(
            "SSE opportunity_expired dispatched for {Email}, OpportunityId={OpportunityId}",
            message.BuyerEmail, message.OpportunityId);
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
