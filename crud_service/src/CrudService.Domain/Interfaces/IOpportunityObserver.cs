namespace CrudService.Domain.Interfaces;

using CrudService.Domain.Entities;

public interface IOpportunityObserver
{
    Task OnOpportunityActivatedAsync(WaitlistOpportunity opportunity);
}
