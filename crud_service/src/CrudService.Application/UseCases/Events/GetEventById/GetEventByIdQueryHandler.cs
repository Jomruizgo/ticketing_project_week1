using CrudService.Application.Dtos;
using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;

namespace CrudService.Application.UseCases.Events.GetEventById;

public class GetEventByIdQueryHandler
{
    private readonly IEventRepository _eventRepository;

    public GetEventByIdQueryHandler(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<EventDto?> HandleAsync(GetEventByIdQuery query)
    {
        var @event = await _eventRepository.GetByIdAsync(query.Id);
        return @event == null ? null : MapToDto(@event);
    }

    private static EventDto MapToDto(Event @event) => new()
    {
        Id = @event.Id,
        Name = @event.Name,
        StartsAt = @event.StartsAt,
        AvailableTickets = @event.Tickets.Count(t => t.Status == TicketStatus.Available),
        ReservedTickets = @event.Tickets.Count(t => t.Status == TicketStatus.Reserved),
        PaidTickets = @event.Tickets.Count(t => t.Status == TicketStatus.Paid)
    };
}
