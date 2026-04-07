using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReservationService.Application.UseCases.ProcessExpiration;
using ReservationService.Application.Interfaces;

namespace ReservationService.Infrastructure.Messaging;

public class TicketExpiredConsumer : BackgroundService
{
    private const int MaxConnectionRetries = 24;
    private const int RetrySeconds = 5;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<TicketExpiredConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public TicketExpiredConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMQSettings> settings,
        ILogger<TicketExpiredConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connected = await ConnectWithRetryAsync(stoppingToken);
        if (!connected || _channel is null)
        {
            _logger.LogError("TicketExpiredConsumer could not connect to RabbitMQ after retries.");
            return;
        }

        _logger.LogInformation("Connected to RabbitMQ. Listening on queue: {Queue}", _settings.ExpiredQueueName);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            _logger.LogInformation("Expiration message received: {Json}", json);

            try
            {
                var message = JsonSerializer.Deserialize<ProcessExpirationCommand>(json, JsonOptions);

                if (message is not null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var useCase = scope.ServiceProvider.GetRequiredService<IProcessExpirationUseCase>();
                    var publisher = scope.ServiceProvider.GetRequiredService<IStatusChangedPublisher>();
                    var result = await useCase.HandleAsync(message, stoppingToken);

                    if (result.Success && result.StatusChanged)
                    {
                        await publisher.PublishAsync(message.TicketId, "released", stoppingToken);
                    }
                }

                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expiration message: {Json}", json);
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(_settings.ExpiredQueueName, autoAck: false, consumer, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task<bool> ConnectWithRetryAsync(CancellationToken stoppingToken)
    {
        for (var attempt = 1; attempt <= MaxConnectionRetries; attempt++)
        {
            if (stoppingToken.IsCancellationRequested) return false;

            try
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
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "RabbitMQ not ready for TicketExpiredConsumer (attempt {Attempt}/{Max}). Retrying in {Seconds}s...",
                    attempt,
                    MaxConnectionRetries,
                    RetrySeconds);

                await Task.Delay(TimeSpan.FromSeconds(RetrySeconds), stoppingToken);
            }
        }

        return false;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping TicketExpiredConsumer...");

        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }
}
