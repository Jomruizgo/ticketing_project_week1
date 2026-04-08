namespace CrudService.Domain.Interfaces;

using CrudService.Domain.Entities;

public interface IWaitlistOpportunityRepository
{
    Task<WaitlistOpportunity?> FindByIdAsync(long id);
    Task<WaitlistOpportunity?> FindByWaitlistEntryIdAsync(long waitlistEntryId);
    Task<WaitlistOpportunity> AddAsync(WaitlistOpportunity opportunity);
    Task UpdateAsync(WaitlistOpportunity opportunity);
    Task<WaitlistOpportunity?> FindActiveByTicketIdAsync(long ticketId);
}
