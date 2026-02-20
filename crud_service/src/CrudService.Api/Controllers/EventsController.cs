using CrudService.Application.Dtos;
using CrudService.Application.UseCases.Events.CreateEvent;
using CrudService.Application.UseCases.Events.DeleteEvent;
using CrudService.Application.UseCases.Events.GetAllEvents;
using CrudService.Application.UseCases.Events.GetEventById;
using CrudService.Application.UseCases.Events.UpdateEvent;
using CrudService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CrudService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly GetAllEventsQueryHandler _getAllEventsHandler;
    private readonly GetEventByIdQueryHandler _getEventByIdHandler;
    private readonly CreateEventCommandHandler _createEventHandler;
    private readonly UpdateEventCommandHandler _updateEventHandler;
    private readonly DeleteEventCommandHandler _deleteEventHandler;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        GetAllEventsQueryHandler getAllEventsHandler,
        GetEventByIdQueryHandler getEventByIdHandler,
        CreateEventCommandHandler createEventHandler,
        UpdateEventCommandHandler updateEventHandler,
        DeleteEventCommandHandler deleteEventHandler,
        ILogger<EventsController> logger)
    {
        _getAllEventsHandler = getAllEventsHandler;
        _getEventByIdHandler = getEventByIdHandler;
        _createEventHandler = createEventHandler;
        _updateEventHandler = updateEventHandler;
        _deleteEventHandler = deleteEventHandler;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents()
    {
        try
        {
            var events = await _getAllEventsHandler.HandleAsync(new GetAllEventsQuery());
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener eventos");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener eventos");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EventDto>> GetEvent(long id)
    {
        try
        {
            var @event = await _getEventByIdHandler.HandleAsync(new GetEventByIdQuery(id));
            if (@event == null) return NotFound($"Evento {id} no encontrado");
            return Ok(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener evento {EventId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost]
    public async Task<ActionResult<EventDto>> CreateEvent([FromBody] CreateEventRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("El nombre del evento es requerido");

            if (request.StartsAt == default)
                return BadRequest("La fecha de inicio es requerida");

            var @event = await _createEventHandler.HandleAsync(new CreateEventCommand(request.Name, request.StartsAt));
            _logger.LogInformation("Evento creado: {EventId}", @event.Id);
            return CreatedAtAction(nameof(GetEvent), new { id = @event.Id }, @event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear evento");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EventDto>> UpdateEvent(long id, [FromBody] UpdateEventRequest request)
    {
        try
        {
            var updated = await _updateEventHandler.HandleAsync(new UpdateEventCommand(id, request.Name, request.StartsAt));
            return Ok(updated);
        }
        catch (EventNotFoundException)
        {
            return NotFound($"Evento {id} no encontrado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar evento {EventId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(long id)
    {
        try
        {
            var deleted = await _deleteEventHandler.HandleAsync(new DeleteEventCommand(id));
            if (!deleted) return NotFound($"Evento {id} no encontrado");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar evento {EventId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
