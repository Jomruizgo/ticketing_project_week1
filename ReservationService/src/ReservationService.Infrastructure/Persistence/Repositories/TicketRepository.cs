using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReservationService.Domain.Entities;
using ReservationService.Domain.Interfaces;

namespace ReservationService.Infrastructure.Persistence.Repositories;

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
            // HUMAN CHECK:
            // Esta actualización debe permanecer atómica con filtro por versión + estado.
            // Si se elimina cualquiera de estas condiciones se rompe el optimistic locking
            // y pueden confirmarse reservas duplicadas bajo concurrencia.
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

    public async Task<bool> TryReleaseAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default)
    {
        var currentVersion = ticket.Version;

        try
        {
            var affected = await _context.Tickets
                .Where(t => t.Id == ticket.Id && t.Version == currentVersion && t.Status == TicketStatus.Reserved)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.Status, TicketStatus.Released)
                    .SetProperty(t => t.ReservedBy, (string?)null)
                    .SetProperty(t => t.OrderId, (string?)null)
                    .SetProperty(t => t.ReservedAt, (DateTime?)null)
                    .SetProperty(t => t.ExpiresAt, (DateTime?)null)
                    .SetProperty(t => t.Version, currentVersion + 1),
                cancellationToken);

            if (affected == 0)
            {
                _logger.LogWarning(
                    "Failed to release ticket {TicketId}: concurrent modification or not reserved",
                    ticket.Id);
                return false;
            }

            ticket.Status = TicketStatus.Released;
            ticket.ReservedBy = null;
            ticket.OrderId = null;
            ticket.ReservedAt = null;
            ticket.ExpiresAt = null;
            ticket.Version = currentVersion + 1;

            _logger.LogInformation("Ticket {TicketId} released successfully", ticket.Id);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex,
                "Concurrency conflict releasing ticket {TicketId}",
                ticket.Id);
            return false;
        }
    }
}
