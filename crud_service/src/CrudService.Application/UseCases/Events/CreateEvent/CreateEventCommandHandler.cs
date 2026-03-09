using CrudService.Application.Dtos;
using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CrudService.Application.UseCases.Events.CreateEvent;

public class CreateEventCommandHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<CreateEventCommandHandler> _logger;

    public CreateEventCommandHandler(IEventRepository eventRepository, ILogger<CreateEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<EventDto> HandleAsync(CreateEventCommand command)
    {
        var @event = new Event
        {
            Name = command.Name,
            StartsAt = command.StartsAt
        };

        var created = await _eventRepository.AddAsync(@event);
        _logger.LogInformation("Evento creado: {EventId}", created.Id);

        return new EventDto
        {
            Id = created.Id,
            Name = created.Name,
            StartsAt = created.StartsAt,
            AvailableTickets = 0,
            ReservedTickets = 0,
            PaidTickets = 0
        };
    }
}
