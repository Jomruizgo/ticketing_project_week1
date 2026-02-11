using CrudService.Models.DTOs;
using CrudService.Models.Entities;
using CrudService.Repositories;

namespace CrudService.Services;

/// <summary>
/// Servicio para gestionar eventos
/// </summary>
public interface IEventService
{
    Task<IEnumerable<EventDto>> GetAllEventsAsync();
    Task<EventDto?> GetEventByIdAsync(long id);
    Task<EventDto> CreateEventAsync(CreateEventRequest request);
    Task<EventDto> UpdateEventAsync(long id, UpdateEventRequest request);
    Task<bool> DeleteEventAsync(long id);
}

/// <summary>
/// Implementación del servicio de eventos
/// </summary>
public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<EventService> _logger;

    public EventService(
        IEventRepository eventRepository,
        ITicketRepository ticketRepository,
        ILogger<EventService> logger)
    {
        _eventRepository = eventRepository;
        _ticketRepository = ticketRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<EventDto>> GetAllEventsAsync()
    {
        var events = await _eventRepository.GetAllAsync();
        return events.Select(MapToDto);
    }

    public async Task<EventDto?> GetEventByIdAsync(long id)
    {
        var @event = await _eventRepository.GetByIdAsync(id);
        return @event == null ? null : MapToDto(@event);
    }

    public async Task<EventDto> CreateEventAsync(CreateEventRequest request)
    {
        var @event = new Event
        {
            Name = request.Name,
            StartsAt = request.StartsAt
        };

        var created = await _eventRepository.AddAsync(@event);
        _logger.LogInformation("Evento creado: {EventId}", created.Id);
        return MapToDto(created);
    }

    public async Task<EventDto> UpdateEventAsync(long id, UpdateEventRequest request)
    {
        var @event = await _eventRepository.GetByIdAsync(id);
        if (@event == null)
            throw new KeyNotFoundException($"Evento {id} no encontrado");

        if (!string.IsNullOrEmpty(request.Name))
            @event.Name = request.Name;

        if (request.StartsAt.HasValue)
            @event.StartsAt = request.StartsAt.Value;

        var updated = await _eventRepository.UpdateAsync(@event);
        _logger.LogInformation("Evento actualizado: {EventId}", id);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteEventAsync(long id)
    {
        var deleted = await _eventRepository.DeleteAsync(id);
        if (deleted)
            _logger.LogInformation("Evento eliminado: {EventId}", id);
        return deleted;
    }

    private EventDto MapToDto(Event @event)
    {
        var tickets = @event.Tickets;
        return new EventDto
        {
            Id = @event.Id,
            Name = @event.Name,
            StartsAt = @event.StartsAt,
            AvailableTickets = tickets.Count(t => t.Status == TicketStatus.Available),
            ReservedTickets = tickets.Count(t => t.Status == TicketStatus.Reserved),
            PaidTickets = tickets.Count(t => t.Status == TicketStatus.Paid)
        };
    }
}
