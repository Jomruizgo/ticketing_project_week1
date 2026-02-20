using MsPaymentService.Domain.Entities;

namespace MsPaymentService.Domain.Interfaces;

public interface ITicketHistoryRepository
{
    Task AddAsync(TicketHistory history);
    Task<List<TicketHistory>> GetByTicketIdAsync(long ticketId);
}
