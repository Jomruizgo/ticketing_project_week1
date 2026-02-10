namespace MsPaymentService.Worker.Models.Entities;

public enum TicketStatus
{
    Available,
    Reserved,
    Paid,
    Released,
    Cancelled
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
    public int Version { get; set; } // Optimistic concurrency
    
    // Navigation properties
    public Event Event { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();
}