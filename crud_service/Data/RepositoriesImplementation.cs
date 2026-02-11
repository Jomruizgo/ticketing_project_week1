using CrudService.Models.Entities;
using CrudService.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrudService.Data;

/// <summary>
/// Implementación del repositorio de eventos
/// </summary>
public class EventRepository : IEventRepository
{
    private readonly TicketingDbContext _context;

    public EventRepository(TicketingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Event>> GetAllAsync()
    {
        return await _context.Events
            .Include(e => e.Tickets)
            .ToListAsync();
    }

    public async Task<Event?> GetByIdAsync(long id)
    {
        return await _context.Events
            .Include(e => e.Tickets)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

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
    {
        await _context.SaveChangesAsync();
    }
}

/// <summary>
/// Implementación del repositorio de tickets
/// </summary>
public class TicketRepository : ITicketRepository
{
    private readonly TicketingDbContext _context;

    public TicketRepository(TicketingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Ticket>> GetByEventIdAsync(long eventId)
    {
        return await _context.Tickets
            .Where(t => t.EventId == eventId)
            .Include(t => t.Payments)
            .Include(t => t.History)
            .ToListAsync();
    }

    public async Task<Ticket?> GetByIdAsync(long id)
    {
        return await _context.Tickets
            .Include(t => t.Payments)
            .Include(t => t.History)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

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
    {
        return await _context.Tickets
            .CountAsync(t => t.Status == status);
    }

    public async Task<IEnumerable<Ticket>> GetExpiredAsync(DateTime now)
    {
        return await _context.Tickets
            .Where(t => t.Status == TicketStatus.Reserved && t.ExpiresAt <= now)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

/// <summary>
/// Implementación del repositorio de pagos
/// </summary>
public class PaymentRepository : IPaymentRepository
{
    private readonly TicketingDbContext _context;

    public PaymentRepository(TicketingDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(long id)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Payment> AddAsync(Payment payment)
    {
        await _context.Payments.AddAsync(payment);
        await SaveChangesAsync();
        return payment;
    }

    public async Task<Payment> UpdateAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        await SaveChangesAsync();
        return payment;
    }

    public async Task<IEnumerable<Payment>> GetByTicketIdAsync(long ticketId)
    {
        return await _context.Payments
            .Where(p => p.TicketId == ticketId)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

/// <summary>
/// Implementación del repositorio de historial
/// </summary>
public class TicketHistoryRepository : ITicketHistoryRepository
{
    private readonly TicketingDbContext _context;

    public TicketHistoryRepository(TicketingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TicketHistory>> GetByTicketIdAsync(long ticketId)
    {
        return await _context.TicketHistories
            .Where(h => h.TicketId == ticketId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();
    }

    public async Task<TicketHistory> AddAsync(TicketHistory history)
    {
        await _context.TicketHistories.AddAsync(history);
        await SaveChangesAsync();
        return history;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
