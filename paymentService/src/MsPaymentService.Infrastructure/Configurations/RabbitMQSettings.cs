namespace MsPaymentService.Infrastructure.Configurations;

public class RabbitMQSettings
{
    public string HostName { get; set; } = Environment.GetEnvironmentVariable("RABBITMQ_HOSTNAME") ?? "localhost";
    public int Port { get; set; } = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var port) ? port : 5672;
    public string UserName { get; set; } = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "guest";
    public string Password { get; set; } = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";
    public string VirtualHost { get; set; } = Environment.GetEnvironmentVariable("RABBITMQ_VHOST") ?? "/";
    public string ExchangeName { get; set; } = "tickets";
    public string ApprovedQueueName { get; set; } = string.Empty;
    public string RejectedQueueName { get; set; } = string.Empty;
    public string RequestedQueueName { get; set; } = string.Empty;
    public ushort PrefetchCount { get; set; } = 10;
}
