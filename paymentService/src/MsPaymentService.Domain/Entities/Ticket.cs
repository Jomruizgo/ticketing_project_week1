namespace MsPaymentService.Domain.Entities;

public enum TicketStatus
{
    available,
    reserved,
    paid,
    released,
    cancelled
}

public class Ticket
{
    public long Id { get; set; }
    public long EventId { get; set; }
    public TicketStatus Status { get; set; }
    public DateTime? ReservedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? OrderId { get; set; }
    public string? ReservedBy { get; set; }
    public int Version { get; set; }
    public Event Event { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();
}
