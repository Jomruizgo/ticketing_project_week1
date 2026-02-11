using Microsoft.EntityFrameworkCore;
using MsPaymentService.Worker.Data;
using MsPaymentService.Worker.Models.Entities;

namespace MsPaymentService.Worker.Repositories;

/// <summary>
/// Implementación del repositorio de tickets usando Entity Framework Core.
/// Proporciona acceso a datos con soporte para concurrencia optimista y bloqueo pesimista.
/// </summary>
public class TicketRepository : ITicketRepository
{
    private readonly PaymentDbContext _context;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="TicketRepository"/>.
    /// </summary>
    /// <param name="context">Contexto de base de datos de Entity Framework.</param>
    public TicketRepository(PaymentDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<Ticket?> GetByIdAsync(long id)
    {
        return await _context.Tickets
            .Include(t => t.Payments)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    /// <inheritdoc/>
    public async Task<Ticket?> GetByIdForUpdateAsync(long id)
    {
        return await _context.Tickets
            .FromSqlRaw("SELECT * FROM tickets WHERE id = {0} FOR UPDATE", id)
            .Include(t => t.Payments)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(Ticket ticket)
    {
        try
        {
            var rowsAffected = await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE tickets 
                SET status = {0}, paid_at = {1}, version = version + 1
                WHERE id = {2} AND version = {3}",
                new object[] { 
                    ticket.Status.ToString().ToLower(), 
                    ticket.PaidAt ?? (object)DBNull.Value, 
                    ticket.Id, 
                    ticket.Version 
                });

            if (rowsAffected == 0)
            {
                throw new DbUpdateConcurrencyException("Ticket was modified by another process");
            }

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

    /// <inheritdoc/>
    public async Task<List<Ticket>> GetExpiredReservedTicketsAsync(DateTime expirationThreshold)
    {
        return await _context.Tickets
            .Where(t => t.Status == TicketStatus.reserved && 
                       t.ExpiresAt != null && 
                       t.ExpiresAt < expirationThreshold)
            .ToListAsync();
    }
}