namespace CrudService.Domain.Entities;

using CrudService.Domain.Enums;

public class WaitlistEntry
{
    public long Id { get; set; }
    public long EventId { get; set; }
    public string BuyerEmail { get; set; } = null!;
    public WaitlistEntryStatus Status { get; set; } = WaitlistEntryStatus.Active;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public Event Event { get; set; } = null!;
}
