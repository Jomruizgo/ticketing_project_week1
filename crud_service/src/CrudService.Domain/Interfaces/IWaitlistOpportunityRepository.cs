namespace CrudService.Domain.Interfaces;

using CrudService.Domain.Entities;

public interface IWaitlistOpportunityRepository
{
    Task<WaitlistOpportunity?> FindByWaitlistEntryIdAsync(long waitlistEntryId);
}
