using CrudService.Domain.Entities;

namespace CrudService.Domain.Interfaces;

public interface ITicketHistoryRepository
{
    Task<IEnumerable<TicketHistory>> GetByTicketIdAsync(long ticketId);
    Task<TicketHistory> AddAsync(TicketHistory history);
    Task SaveChangesAsync();
}
