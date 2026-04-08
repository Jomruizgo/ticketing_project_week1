using System.Text;
using System.Text.Json;
using CrudService.Application.UseCases.Waitlist.AssignOpportunity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CrudService.Infrastructure.Messaging;

public class TicketReleasedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<TicketReleasedConsumer> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions PublishOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public TicketReleasedConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMQSettings> settings,
        ILogger<TicketReleasedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
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
                _channel.BasicQos(0, 1, false);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += OnMessageAsync;

                _channel.BasicConsume(
                    queue: "q.ticket.released",
                    autoAck: false,
                    consumer: consumer);

                _logger.LogInformation("TicketReleasedConsumer listening on q.ticket.released");
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

        _logger.LogError("TicketReleasedConsumer could not connect to RabbitMQ after {Max} attempts.", maxRetries);
    }

    private async Task OnMessageAsync(object sender, BasicDeliverEventArgs args)
    {
        try
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            var released = JsonSerializer.Deserialize<TicketReleasedEvent>(json, JsonOptions);

            if (released is null || released.TicketId <= 0 || released.EventId <= 0)
            {
                _logger.LogWarning("Invalid ticket.released payload: {Payload}", json);
                _channel?.BasicAck(args.DeliveryTag, false);
                return;
            }

            _logger.LogInformation(
                "ticket.released received. TicketId={TicketId}, EventId={EventId}",
                released.TicketId, released.EventId);

            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<IAssignOpportunityUseCase>();

            var command = new AssignOpportunityCommand(released.TicketId, released.EventId);
            var result = await useCase.HandleAsync(command);

            if (result.Type is AssignOpportunityResultType.NoEligible or AssignOpportunityResultType.AllFailed)
            {
                PublishReturnToInventory(released.TicketId, released.EventId);
            }

            _channel?.BasicAck(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Technical failure processing ticket.released");
            _channel?.BasicNack(args.DeliveryTag, false, requeue: false);
        }
    }

    private void PublishReturnToInventory(long ticketId, long eventId)
    {
        var payload = new
        {
            ticketId,
            eventId,
            returnedAt = DateTime.UtcNow
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, PublishOptions));
        _channel?.BasicPublish(
            exchange: "tickets",
            routingKey: "ticket.returned_to_inventory",
            basicProperties: null,
            body: body);

        _logger.LogInformation(
            "Published ticket.returned_to_inventory for TicketId={TicketId}, EventId={EventId}",
            ticketId, eventId);
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
