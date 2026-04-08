namespace CrudService.Domain.Entities;

using CrudService.Domain.Enums;

public class WaitlistOpportunity
{
    public long Id { get; set; }
    public long WaitlistEntryId { get; set; }
    public long TicketId { get; set; }
    public WaitlistOpportunityStatus Status { get; set; } = WaitlistOpportunityStatus.Pending;
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public WaitlistEntry WaitlistEntry { get; set; } = null!;
    public Ticket Ticket { get; set; } = null!;
}
