namespace Producer.Infrastructure.Messaging;

public class RabbitMQSettings
{
    public const string SectionName = "RabbitMQ";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "tickets";
    public string TicketReservedRoutingKey { get; set; } = "ticket.reserved";
    public string PaymentApprovedRoutingKey { get; set; } = "ticket.payments.approved";
    public string PaymentRejectedRoutingKey { get; set; } = "ticket.payments.rejected";
    public string PaymentRequestedRoutingKey { get; set; } = "ticket.payment.requested";
}
