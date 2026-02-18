using ReservationService.Domain.Entities;

namespace ReservationService.Domain.Interfaces;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(long ticketId, CancellationToken cancellationToken = default);
    Task<bool> TryReserveAsync(Ticket ticket, string reservedBy, string orderId, DateTime expiresAt, CancellationToken cancellationToken = default);
}
