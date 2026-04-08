namespace CrudService.Domain.Events;

public record OpportunityActivatedEvent(
    long OpportunityId,
    long EventId,
    string EventName,
    string BuyerEmail,
    DateTime ActivatedAt,
    DateTime ExpiresAt);
