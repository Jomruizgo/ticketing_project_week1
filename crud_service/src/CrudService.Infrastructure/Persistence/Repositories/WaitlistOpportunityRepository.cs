namespace CrudService.Infrastructure.Persistence.Repositories;

using CrudService.Domain.Entities;
using CrudService.Domain.Enums;
using CrudService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class WaitlistOpportunityRepository : IWaitlistOpportunityRepository
{
    private readonly TicketingDbContext _context;

    public WaitlistOpportunityRepository(TicketingDbContext context)
    {
        _context = context;
    }

    public async Task<WaitlistOpportunity?> FindByWaitlistEntryIdAsync(long waitlistEntryId)
    {
        try
        {
            return await _context.WaitlistOpportunities
                .Where(o => o.WaitlistEntryId == waitlistEntryId)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();
        }
        catch (Exception)
        {
            // Table may not exist yet — return null gracefully
            return null;
        }
    }

    public async Task<WaitlistOpportunity> AddAsync(WaitlistOpportunity opportunity)
    {
        _context.WaitlistOpportunities.Add(opportunity);
        await _context.SaveChangesAsync();
        return opportunity;
    }

    public async Task UpdateAsync(WaitlistOpportunity opportunity)
    {
        _context.WaitlistOpportunities.Update(opportunity);
        await _context.SaveChangesAsync();
    }

    public async Task<WaitlistOpportunity?> FindActiveByTicketIdAsync(long ticketId)
    {
        return await _context.WaitlistOpportunities
            .FirstOrDefaultAsync(o => o.TicketId == ticketId
                                   && o.Status == WaitlistOpportunityStatus.Active);
    }
}
