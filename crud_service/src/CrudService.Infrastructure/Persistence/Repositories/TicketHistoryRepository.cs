using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CrudService.Infrastructure.Persistence.Repositories;

public class TicketHistoryRepository : ITicketHistoryRepository
{
    private readonly TicketingDbContext _context;

    public TicketHistoryRepository(TicketingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TicketHistory>> GetByTicketIdAsync(long ticketId)
        => await _context.TicketHistories
            .Where(h => h.TicketId == ticketId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();

    public async Task<TicketHistory> AddAsync(TicketHistory history)
    {
        await _context.TicketHistories.AddAsync(history);
        await SaveChangesAsync();
        return history;
    }

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}
