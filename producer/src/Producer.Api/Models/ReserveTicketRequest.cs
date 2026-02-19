namespace Producer.Api.Models;

public class ReserveTicketRequest
{
    public long EventId { get; set; }
    public long TicketId { get; set; }
    public string? OrderId { get; set; }
    public string? ReservedBy { get; set; }
    public int ExpiresInSeconds { get; set; } = 300;
}
