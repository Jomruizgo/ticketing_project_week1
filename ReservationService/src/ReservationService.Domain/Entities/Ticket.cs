namespace ReservationService.Domain.Entities;

public class Ticket
{
    public long Id { get; set; }
    public long EventId { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Available;
    public DateTime? ReservedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? OrderId { get; set; }
    public string? ReservedBy { get; set; }
    public int Version { get; set; } = 0;
}
