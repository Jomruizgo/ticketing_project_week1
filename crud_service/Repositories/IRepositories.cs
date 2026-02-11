using CrudService.Models.Entities;

namespace CrudService.Repositories;

/// <summary>
/// Interfaz para repositorio de eventos
/// </summary>
public interface IEventRepository
{
    Task<IEnumerable<Event>> GetAllAsync();
    Task<Event?> GetByIdAsync(long id);
    Task<Event> AddAsync(Event @event);
    Task<Event> UpdateAsync(Event @event);
    Task<bool> DeleteAsync(long id);
    Task SaveChangesAsync();
}

/// <summary>
/// Interfaz para repositorio de tickets
/// </summary>
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

/// <summary>
/// Interfaz para repositorio de pagos
/// </summary>
public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(long id);
    Task<Payment> AddAsync(Payment payment);
    Task<Payment> UpdateAsync(Payment payment);
    Task<IEnumerable<Payment>> GetByTicketIdAsync(long ticketId);
    Task SaveChangesAsync();
}

/// <summary>
/// Interfaz para repositorio de historial
/// </summary>
public interface ITicketHistoryRepository
{
    Task<IEnumerable<TicketHistory>> GetByTicketIdAsync(long ticketId);
    Task<TicketHistory> AddAsync(TicketHistory history);
    Task SaveChangesAsync();
}
