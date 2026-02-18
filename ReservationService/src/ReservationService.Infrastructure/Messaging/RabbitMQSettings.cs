namespace ReservationService.Infrastructure.Messaging;

public class RabbitMQSettings
{
    public const string SectionName = "RabbitMQ";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string QueueName { get; set; } = "q.ticket.reserved";
    public string ExchangeName { get; set; } = "tickets";
    public string RoutingKey { get; set; } = "ticket.reserved";
}
