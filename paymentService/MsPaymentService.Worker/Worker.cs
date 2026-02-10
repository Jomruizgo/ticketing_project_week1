using MsPaymentService.Worker.Messaging.RabbitMQ;

namespace MsPaymentService.Worker;

public class Worker : BackgroundService
{
    private readonly TicketPaymentConsumer _consumer;
    private readonly ILogger<Worker> _logger;

    public Worker(TicketPaymentConsumer consumer, ILogger<Worker> logger)
    {
        _consumer = consumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            _consumer.Start("ticket.payments.approved");
            _consumer.Start("ticket.payments.rejected");
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
