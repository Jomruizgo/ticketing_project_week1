using CrudService.Application.Dtos;
using CrudService.Domain.Entities;
using CrudService.Domain.Exceptions;
using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CrudService.Application.UseCases.Events.UpdateEvent;

public class UpdateEventCommandHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<UpdateEventCommandHandler> _logger;

    public UpdateEventCommandHandler(IEventRepository eventRepository, ILogger<UpdateEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<EventDto> HandleAsync(UpdateEventCommand command)
    {
        var @event = await _eventRepository.GetByIdAsync(command.Id)
            ?? throw new EventNotFoundException(command.Id);

        if (!string.IsNullOrEmpty(command.Name))
            @event.Name = command.Name;

        if (command.StartsAt.HasValue)
            @event.StartsAt = command.StartsAt.Value;

        var updated = await _eventRepository.UpdateAsync(@event);
        _logger.LogInformation("Evento actualizado: {EventId}", command.Id);

        return new EventDto
        {
            Id = updated.Id,
            Name = updated.Name,
            StartsAt = updated.StartsAt,
            AvailableTickets = updated.Tickets.Count(t => t.Status == TicketStatus.Available),
            ReservedTickets = updated.Tickets.Count(t => t.Status == TicketStatus.Reserved),
            PaidTickets = updated.Tickets.Count(t => t.Status == TicketStatus.Paid)
        };
    }
}
