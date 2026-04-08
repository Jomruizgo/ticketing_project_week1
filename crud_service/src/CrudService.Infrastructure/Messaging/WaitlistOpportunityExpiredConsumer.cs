using System.Text;
using System.Text.Json;
using CrudService.Application.UseCases.Waitlist.ExpireOpportunity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CrudService.Infrastructure.Messaging;

public class WaitlistOpportunityExpiredConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<WaitlistOpportunityExpiredConsumer> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WaitlistOpportunityExpiredConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMQSettings> settings,
        ILogger<WaitlistOpportunityExpiredConsumer> logger)
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
                    queue: "q.waitlist.opportunity.expired",
                    autoAck: false,
                    consumer: consumer);

                _logger.LogInformation("WaitlistOpportunityExpiredConsumer listening on q.waitlist.opportunity.expired");
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

        _logger.LogError("WaitlistOpportunityExpiredConsumer could not connect to RabbitMQ after {Max} attempts.", maxRetries);
    }

    private async Task OnMessageAsync(object sender, BasicDeliverEventArgs args)
    {
        try
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            var message = JsonSerializer.Deserialize<OpportunityExpiredMessage>(json, JsonOptions);

            if (message is null || message.OpportunityId <= 0)
            {
                _logger.LogWarning("Invalid opportunity expired payload: {Payload}", json);
                _channel?.BasicAck(args.DeliveryTag, false);
                return;
            }

            _logger.LogInformation(
                "Opportunity expired message received. OpportunityId={OpportunityId}",
                message.OpportunityId);

            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<IExpireOpportunityUseCase>();

            var command = new ExpireOpportunityCommand(message.OpportunityId);
            var result = await useCase.HandleAsync(command);

            _logger.LogInformation(
                "Opportunity expiration processed. OpportunityId={OpportunityId}, Result={Result}",
                message.OpportunityId, result.Type);

            _channel?.BasicAck(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Technical failure processing opportunity expired message");
            _channel?.BasicNack(args.DeliveryTag, false, requeue: false);
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }

    private record OpportunityExpiredMessage(long OpportunityId);
}
