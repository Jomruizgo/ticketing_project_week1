using Microsoft.EntityFrameworkCore;
using ReservationService.Worker.Data;
using ReservationService.Worker.Models;

namespace ReservationService.Worker.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly TicketingDbContext _context;
    private readonly ILogger<TicketRepository> _logger;

    public TicketRepository(TicketingDbContext context, ILogger<TicketRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Ticket?> GetByIdAsync(long ticketId, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets.FindAsync([ticketId], cancellationToken);
    }

    // 🛡 HUMAN CHECK:
    // Se usa optimistic locking con el campo Version para evitar race conditions.
    // Si dos requests intentan reservar el mismo ticket simultáneamente,
    // solo uno tendrá éxito (el que tenga la versión correcta).
    public async Task<bool> TryReserveAsync(
        Ticket ticket,
        string reservedBy,
        string orderId,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        var currentVersion = ticket.Version;

        ticket.Status = TicketStatus.Reserved;
        ticket.ReservedBy = reservedBy;
        ticket.OrderId = orderId;
        ticket.ReservedAt = DateTime.UtcNow;
        ticket.ExpiresAt = expiresAt;
        ticket.Version = currentVersion + 1;

        try
        {
            // UPDATE con WHERE version = currentVersion (optimistic locking)
            var affected = await _context.Tickets
                .Where(t => t.Id == ticket.Id && t.Version == currentVersion && t.Status == TicketStatus.Available)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.Status, TicketStatus.Reserved)
                    .SetProperty(t => t.ReservedBy, reservedBy)
                    .SetProperty(t => t.OrderId, orderId)
                    .SetProperty(t => t.ReservedAt, DateTime.UtcNow)
                    .SetProperty(t => t.ExpiresAt, expiresAt)
                    .SetProperty(t => t.Version, currentVersion + 1),
                cancellationToken);

            if (affected == 0)
            {
                _logger.LogWarning(
                    "Failed to reserve ticket {TicketId}: concurrent modification or not available",
                    ticket.Id);
                return false;
            }

            _logger.LogInformation(
                "Ticket {TicketId} reserved successfully for {ReservedBy} until {ExpiresAt}",
                ticket.Id, reservedBy, expiresAt);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex,
                "Concurrency conflict reserving ticket {TicketId}",
                ticket.Id);
            return false;
        }
    }
}
