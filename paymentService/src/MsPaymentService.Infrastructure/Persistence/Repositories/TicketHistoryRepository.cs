using Microsoft.EntityFrameworkCore;
using MsPaymentService.Domain.Entities;
using MsPaymentService.Domain.Interfaces;

namespace MsPaymentService.Infrastructure.Persistence.Repositories;

public class TicketHistoryRepository : ITicketHistoryRepository
{
    private readonly PaymentDbContext _context;

    public TicketHistoryRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(TicketHistory history)
    {
        _context.TicketHistory.Add(history);
        await _context.SaveChangesAsync();
    }

    public async Task<List<TicketHistory>> GetByTicketIdAsync(long ticketId)
    {
        return await _context.TicketHistory
            .Where(h => h.TicketId == ticketId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();
    }
}
