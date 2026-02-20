using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CrudService.Infrastructure.Persistence.Repositories;

public class EventRepository : IEventRepository
{
    private readonly TicketingDbContext _context;

    public EventRepository(TicketingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Event>> GetAllAsync()
        => await _context.Events.Include(e => e.Tickets).ToListAsync();

    public async Task<Event?> GetByIdAsync(long id)
        => await _context.Events.Include(e => e.Tickets).FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Event> AddAsync(Event @event)
    {
        await _context.Events.AddAsync(@event);
        await SaveChangesAsync();
        return @event;
    }

    public async Task<Event> UpdateAsync(Event @event)
    {
        _context.Events.Update(@event);
        await SaveChangesAsync();
        return @event;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var @event = await GetByIdAsync(id);
        if (@event == null) return false;
        _context.Events.Remove(@event);
        await SaveChangesAsync();
        return true;
    }

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}
