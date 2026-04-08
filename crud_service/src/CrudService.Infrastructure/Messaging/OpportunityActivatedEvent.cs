namespace CrudService.Infrastructure.Messaging;

public class OpportunityActivatedEvent
{
    public long OpportunityId { get; set; }
    public long WaitlistEntryId { get; set; }
    public long TicketId { get; set; }
    public long EventId { get; set; }
    public string BuyerEmail { get; set; } = null!;
    public DateTime ActivatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
