namespace Producer.Domain.Events;

public class TicketReservedEvent
{
    public long TicketId { get; set; }
    public long EventId { get; set; }
    public string? OrderId { get; set; }
    public string? ReservedBy { get; set; }
    public int ReservationDurationSeconds { get; set; } = 300;
    public DateTime PublishedAt { get; set; }
}
