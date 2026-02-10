using MsPaymentService.Worker.Models.Entities;

namespace MsPaymentService.Worker.Repositories;

public interface ITicketHistoryRepository
{
    Task AddAsync(TicketHistory history);
    Task<List<TicketHistory>> GetByTicketIdAsync(long ticketId);
}