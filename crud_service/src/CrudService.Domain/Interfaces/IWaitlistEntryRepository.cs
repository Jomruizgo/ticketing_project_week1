namespace CrudService.Domain.Interfaces;

using CrudService.Domain.Entities;

public interface IWaitlistEntryRepository
{
    Task<bool> ExistsActiveAsync(long eventId, string buyerEmail);
    Task<WaitlistEntry> AddAsync(WaitlistEntry entry);
}
