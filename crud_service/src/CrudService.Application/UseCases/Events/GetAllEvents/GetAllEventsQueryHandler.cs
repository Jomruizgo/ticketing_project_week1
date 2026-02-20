using CrudService.Application.Dtos;
using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;

namespace CrudService.Application.UseCases.Events.GetAllEvents;

public class GetAllEventsQueryHandler
{
    private readonly IEventRepository _eventRepository;

    public GetAllEventsQueryHandler(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<IEnumerable<EventDto>> HandleAsync(GetAllEventsQuery query)
    {
        var events = await _eventRepository.GetAllAsync();
        return events.Select(MapToDto);
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
