namespace Producer.Configurations;

/// <summary>
/// Configuración de RabbitMQ
/// </summary>
public class RabbitMQOptions
{
    /// <summary>
    /// Nombre de la sección en appsettings
    /// </summary>
    public const string SectionName = "RabbitMQ";

    /// <summary>
    /// Hostname de RabbitMQ
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Puerto AMQP de RabbitMQ
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Usuario de RabbitMQ
    /// </summary>
    public string Username { get; set; } = "guest";

    /// <summary>
    /// Contraseña de RabbitMQ
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Virtual host
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Nombre del exchange
    /// </summary>
    public string ExchangeName { get; set; } = "tickets";

    /// <summary>
    /// Routing key para ticket.reserved
    /// </summary>
    public string TicketReservedRoutingKey { get; set; } = "ticket.reserved";

    /// <summary>
    /// Routing key para ticket.payments.approved
    /// </summary>
    public string PaymentApprovedRoutingKey { get; set; } = "ticket.payments.approved";

    /// <summary>
    /// Routing key para ticket.payments.rejected
    /// </summary>
    public string PaymentRejectedRoutingKey { get; set; } = "ticket.payments.rejected";
}
