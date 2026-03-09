using Microsoft.EntityFrameworkCore;
using MsPaymentService.Domain.Entities;
using MsPaymentService.Domain.Interfaces;

namespace MsPaymentService.Infrastructure.Persistence.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly PaymentDbContext _context;

    public TicketRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Ticket?> GetByIdAsync(long id)
    {
        return await _context.Tickets
            .Include(t => t.Payments)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Ticket?> GetByIdForUpdateAsync(long id)
    {
        return await _context.Tickets
            .FromSqlRaw("SELECT * FROM tickets WHERE id = {0} FOR UPDATE", id)
            .Include(t => t.Payments)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(Ticket ticket)
    {
        try
        {
            var rowsAffected = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE tickets
                SET status = {0}, paid_at = {1}, version = version + 1
                WHERE id = {2} AND version = {3}",
                new object[]
                {
                    ticket.Status,
                    ticket.PaidAt ?? (object)DBNull.Value,
                    ticket.Id,
                    ticket.Version
                });

            if (rowsAffected == 0)
            {
                throw new DbUpdateConcurrencyException("Ticket was modified by another process");
            }

            _context.Entry(ticket).State = EntityState.Detached;
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<List<Ticket>> GetExpiredReservedTicketsAsync(DateTime expirationThreshold)
    {
        return await _context.Tickets
            .Where(t => t.Status == TicketStatus.reserved &&
                        t.ExpiresAt != null &&
                        t.ExpiresAt < expirationThreshold)
            .ToListAsync();
    }
}
