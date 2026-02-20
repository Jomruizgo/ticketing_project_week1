namespace CrudService.Domain.Entities;

public class TicketHistory
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public TicketStatus OldStatus { get; set; }
    public TicketStatus NewStatus { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
    public Ticket Ticket { get; set; } = null!;
}
