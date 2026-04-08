namespace CrudService.Domain.Entities;

using CrudService.Domain.Enums;
using CrudService.Domain.Exceptions;

public class WaitlistOpportunity
{
    private static readonly Dictionary<WaitlistOpportunityStatus, HashSet<WaitlistOpportunityStatus>> AllowedTransitions = new()
    {
        [WaitlistOpportunityStatus.Pending] = new() { WaitlistOpportunityStatus.Active, WaitlistOpportunityStatus.Failed },
        [WaitlistOpportunityStatus.Active]  = new() { WaitlistOpportunityStatus.Consumed, WaitlistOpportunityStatus.Expired },
    };

    public long Id { get; set; }
    public long WaitlistEntryId { get; set; }
    public long TicketId { get; set; }
    public WaitlistOpportunityStatus Status { get; set; } = WaitlistOpportunityStatus.Pending;
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public WaitlistEntry WaitlistEntry { get; set; } = null!;
    public Ticket Ticket { get; set; } = null!;

    public void TransitionTo(WaitlistOpportunityStatus newStatus)
    {
        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
            throw new InvalidOpportunityTransitionException(Status, newStatus);

        Status = newStatus;

        if (newStatus == WaitlistOpportunityStatus.Active)
            ActivatedAt = DateTime.UtcNow;
    }
}
