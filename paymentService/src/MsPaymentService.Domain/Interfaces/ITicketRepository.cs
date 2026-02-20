using MsPaymentService.Domain.Entities;

namespace MsPaymentService.Domain.Interfaces;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(long id);
    Task<Ticket?> GetByIdForUpdateAsync(long id);
    Task<bool> UpdateAsync(Ticket ticket);
    Task<List<Ticket>> GetExpiredReservedTicketsAsync(DateTime expirationThreshold);
}
