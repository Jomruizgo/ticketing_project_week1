namespace CrudService.Domain.Interfaces;

using CrudService.Domain.Entities;
using CrudService.Domain.Enums;

public interface IWaitlistEntryRepository
{
    Task<bool> ExistsActiveAsync(long eventId, string buyerEmail);
    Task<WaitlistEntry> AddAsync(WaitlistEntry entry);
    Task<WaitlistEntry?> FindActiveByEventAndEmailAsync(long eventId, string buyerEmail);
    Task<IReadOnlyList<WaitlistEntry>> GetActiveEntriesByEventAsync(long eventId);
    Task UpdateStatusAsync(long entryId, WaitlistEntryStatus newStatus);
}
