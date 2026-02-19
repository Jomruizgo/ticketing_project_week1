namespace MsPaymentService.Worker.Configurations;

/// <summary>
/// Configuración de conexión y consumo de RabbitMQ.
/// La topología (exchange, colas, bindings) se define en scripts/ (setup-rabbitmq.sh, rabbitmq-definitions.json).
/// Este MS solo consume; solo necesita conexión y los nombres de las colas a escuchar.
/// </summary>
public class RabbitMQSettings
{
    public string HostName { get; set; } = Environment.GetEnvironmentVariable("RABBITMQ_HOSTNAME") ?? "localhost";
    public int Port { get; set; } = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var port) ? port : 5672;
    public string UserName { get; set; } = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "guest";
    public string Password { get; set; } = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";
    public string VirtualHost { get; set; } = Environment.GetEnvironmentVariable("RABBITMQ_VHOST") ?? "/";

    /// <summary>Nombre del exchange.</summary>
    public string ExchangeName { get; set; } = "tickets";

    /// <summary>Nombre de la cola de pagos aprobados.</summary>
    public string ApprovedQueueName { get; set; } = string.Empty;

    /// <summary>Nombre de la cola de pagos rechazados.</summary>
    public string RejectedQueueName { get; set; } = string.Empty;

    /// <summary>Nombre de la cola de solicitudes de pago.</summary>
    public string RequestedQueueName { get; set; } = string.Empty;

    /// <summary>Prefetch por canal.</summary>
    public ushort PrefetchCount { get; set; } = 10;
}
