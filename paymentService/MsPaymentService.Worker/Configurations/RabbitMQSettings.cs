namespace MsPaymentService.Worker.Configurations;

/// <summary>
/// Configuración de conexión y consumo de RabbitMQ.
/// La topología (exchange, colas, bindings) se define en scripts/ (setup-rabbitmq.sh, rabbitmq-definitions.json).
/// Este MS solo consume; solo necesita conexión y los nombres de las colas a escuchar.
/// </summary>
public class RabbitMQSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";

    /// <summary>Nombre de la cola de pagos aprobados. Valor en appsettings.json (RabbitMQ:ApprovedQueueName) o env RabbitMQ__ApprovedQueueName.</summary>
    public string ApprovedQueueName { get; set; } = string.Empty;

    /// <summary>Nombre de la cola de pagos rechazados. Valor en appsettings.json (RabbitMQ:RejectedQueueName) o env RabbitMQ__RejectedQueueName.</summary>
    public string RejectedQueueName { get; set; } = string.Empty;

    /// <summary>Prefetch por canal (cuántos mensajes sin ACK puede tener en vuelo el consumer).</summary>
    public ushort PrefetchCount { get; set; } = 10;
}
