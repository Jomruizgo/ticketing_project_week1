using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CrudService.Infrastructure.Persistence.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly TicketingDbContext _context;

    public TicketRepository(TicketingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Ticket>> GetByEventIdAsync(long eventId)
        => await _context.Tickets
            .Where(t => t.EventId == eventId)
            .Include(t => t.Payments)
            .Include(t => t.History)
            .ToListAsync();

    public async Task<Ticket?> GetByIdAsync(long id)
        => await _context.Tickets
            .Include(t => t.Payments)
            .Include(t => t.History)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Ticket> AddAsync(Ticket ticket)
    {
        await _context.Tickets.AddAsync(ticket);
        await SaveChangesAsync();
        return ticket;
    }

    public async Task<Ticket> UpdateAsync(Ticket ticket)
    {
        _context.Tickets.Update(ticket);
        await SaveChangesAsync();
        return ticket;
    }

    public async Task<int> CountByStatusAsync(TicketStatus status)
        => await _context.Tickets.CountAsync(t => t.Status == status);

    public async Task<IEnumerable<Ticket>> GetExpiredAsync(DateTime now)
        => await _context.Tickets
            .Where(t => t.Status == TicketStatus.Reserved && t.ExpiresAt <= now)
            .ToListAsync();

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}
