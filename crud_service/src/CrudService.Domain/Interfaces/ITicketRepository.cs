using CrudService.Domain.Entities;

namespace CrudService.Domain.Interfaces;

public interface ITicketRepository
{
    Task<IEnumerable<Ticket>> GetByEventIdAsync(long eventId);
    Task<Ticket?> GetByIdAsync(long id);
    Task<Ticket> AddAsync(Ticket ticket);
    Task<Ticket> UpdateAsync(Ticket ticket);
    Task<int> CountByStatusAsync(TicketStatus status);
    Task<IEnumerable<Ticket>> GetExpiredAsync(DateTime now);
    Task SaveChangesAsync();
}
