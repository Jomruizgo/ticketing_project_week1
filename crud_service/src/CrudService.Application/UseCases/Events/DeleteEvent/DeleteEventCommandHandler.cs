using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CrudService.Application.UseCases.Events.DeleteEvent;

public class DeleteEventCommandHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<DeleteEventCommandHandler> _logger;

    public DeleteEventCommandHandler(IEventRepository eventRepository, ILogger<DeleteEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(DeleteEventCommand command)
    {
        var deleted = await _eventRepository.DeleteAsync(command.Id);
        if (deleted)
            _logger.LogInformation("Evento eliminado: {EventId}", command.Id);
        return deleted;
    }
}
