namespace CrudService.Domain.Interfaces;

using CrudService.Domain.Events;

public interface IOpportunityObserver
{
    Task OnOpportunityActivatedAsync(OpportunityActivatedEvent activatedEvent);
}
