using Microsoft.Extensions.Options;
using MsPaymentService.Infrastructure.Configurations;
using MsPaymentService.Infrastructure.Messaging.RabbitMQ;
using RabbitMQ.Client.Exceptions;

namespace MsPaymentService.Worker;

public class Worker : BackgroundService
{
    private readonly TicketPaymentConsumer _consumer;
    private readonly RabbitMQSettings _rabbitSettings;
    private readonly ILogger<Worker> _logger;

    private const int StartupRetrySeconds = 5;
    private const int StartupMaxRetries = 24;

    public Worker(
        TicketPaymentConsumer consumer,
        IOptions<RabbitMQSettings> rabbitSettings,
        ILogger<Worker> logger)
    {
        _consumer = consumer;
        _rabbitSettings = rabbitSettings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Worker starting at: {Time}", DateTimeOffset.Now);
        }

        for (var attempt = 1; attempt <= StartupMaxRetries; attempt++)
        {
            if (stoppingToken.IsCancellationRequested) return;

            try
            {
                _consumer.Start(_rabbitSettings.RequestedQueueName);
                _consumer.Start(_rabbitSettings.ApprovedQueueName);
                _consumer.Start(_rabbitSettings.RejectedQueueName);
                _logger.LogInformation("Worker connected to RabbitMQ and consuming at: {Time}", DateTimeOffset.Now);
                await Task.Delay(Timeout.Infinite, stoppingToken);
                return;
            }
            catch (BrokerUnreachableException ex)
            {
                _logger.LogWarning(ex,
                    "RabbitMQ not ready (attempt {Attempt}/{Max}). Retrying in {Seconds}s...",
                    attempt, StartupMaxRetries, StartupRetrySeconds);
                await Task.Delay(TimeSpan.FromSeconds(StartupRetrySeconds), stoppingToken);
            }
        }

        _logger.LogError("Could not connect to RabbitMQ after {Max} attempts. Exiting.", StartupMaxRetries);
    }
}
