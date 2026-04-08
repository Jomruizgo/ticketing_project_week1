namespace CrudService.Infrastructure.Persistence.Repositories;

using CrudService.Domain.Entities;
using CrudService.Domain.Enums;
using CrudService.Domain.Exceptions;
using CrudService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public class WaitlistEntryRepository : IWaitlistEntryRepository
{
    private readonly TicketingDbContext _context;

    public WaitlistEntryRepository(TicketingDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsActiveAsync(long eventId, string buyerEmail)
    {
        return await _context.WaitlistEntries
            .AnyAsync(e => e.EventId == eventId
                        && e.BuyerEmail == buyerEmail
                        && e.Status == WaitlistEntryStatus.Active);
    }

    public async Task<WaitlistEntry> AddAsync(WaitlistEntry entry)
    {
        try
        {
            _context.WaitlistEntries.Add(entry);
            await _context.SaveChangesAsync();
            return entry;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx
                                            && pgEx.SqlState == "23505")
        {
            throw new DuplicateWaitlistEntryException();
        }
    }

    public async Task<WaitlistEntry?> FindActiveByEventAndEmailAsync(long eventId, string buyerEmail)
    {
        return await _context.WaitlistEntries
            .Where(e => e.EventId == eventId
                     && e.BuyerEmail == buyerEmail
                     && e.Status == WaitlistEntryStatus.Active)
            .OrderByDescending(e => e.EnrolledAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<WaitlistEntry>> GetActiveEntriesByEventAsync(long eventId)
    {
        return await _context.WaitlistEntries
            .Where(e => e.EventId == eventId && e.Status == WaitlistEntryStatus.Active)
            .ToListAsync();
    }

    public async Task UpdateStatusAsync(long entryId, WaitlistEntryStatus newStatus)
    {
        await _context.WaitlistEntries
            .Where(e => e.Id == entryId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Status, newStatus));
    }
}
