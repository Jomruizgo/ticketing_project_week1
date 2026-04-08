namespace CrudService.Infrastructure.Persistence.Repositories;

using CrudService.Domain.Entities;
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
}
