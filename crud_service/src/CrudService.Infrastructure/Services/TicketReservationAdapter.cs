namespace CrudService.Infrastructure.Services;

using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;
using CrudService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class TicketReservationAdapter : ITicketReservationPort
{
    private readonly TicketingDbContext _context;

    public TicketReservationAdapter(TicketingDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryReserveForWaitlistAsync(long ticketId, string buyerEmail)
    {
        var rowsAffected = await _context.Tickets
            .Where(t => t.Id == ticketId && t.Status == TicketStatus.Released)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, TicketStatus.Reserved)
                .SetProperty(t => t.ReservedBy, buyerEmail)
                .SetProperty(t => t.ReservedAt, DateTime.UtcNow));

        return rowsAffected > 0;
    }
}
