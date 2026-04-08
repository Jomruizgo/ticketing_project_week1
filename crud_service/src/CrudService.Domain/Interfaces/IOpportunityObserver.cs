namespace CrudService.Domain.Interfaces;

using CrudService.Domain.Entities;
using CrudService.Domain.Events;

public interface IOpportunityObserver
{
    Task OnOpportunityActivatedAsync(OpportunityActivatedEvent activatedEvent);
    Task OnOpportunityExpiredAsync(WaitlistOpportunity opportunity);
}
