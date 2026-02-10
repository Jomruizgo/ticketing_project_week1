namespace MsPaymentService.Worker.Configurations;

public class RabbitMQSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    
    // Configuración de colas
    public QueueSettings PaymentApprovedQueue { get; set; } = new();
    public QueueSettings PaymentRejectedQueue { get; set; } = new();
}

public class QueueSettings
{
    public string QueueName { get; set; } = string.Empty;
    public string ExchangeName { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public bool Durable { get; set; } = true;
    public bool AutoDelete { get; set; } = false;
    public ushort PrefetchCount { get; set; } = 10;
}