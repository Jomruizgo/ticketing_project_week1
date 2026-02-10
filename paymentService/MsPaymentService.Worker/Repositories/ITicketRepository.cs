using MsPaymentService.Worker.Models.Entities;

namespace MsPaymentService.Worker.Repositories;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(long id);
    Task<Ticket?> GetByIdForUpdateAsync(long id); // SELECT FOR UPDATE
    Task<bool> UpdateAsync(Ticket ticket);
    Task<List<Ticket>> GetExpiredReservedTicketsAsync(DateTime expirationThreshold);
}