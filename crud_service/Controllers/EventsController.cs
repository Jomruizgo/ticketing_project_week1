using CrudService.Models.DTOs;
using CrudService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrudService.Controllers;

/// <summary>
/// Controlador para gestionar eventos
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IEventService eventService, ILogger<EventsController> logger)
    {
        _eventService = eventService;
        _logger = logger;
    }

    /// <summary>
    /// Obtener todos los eventos
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents()
    {
        try
        {
            var events = await _eventService.GetAllEventsAsync();
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener eventos");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener eventos");
        }
    }

    /// <summary>
    /// Obtener un evento por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EventDto>> GetEvent(long id)
    {
        try
        {
            var @event = await _eventService.GetEventByIdAsync(id);
            if (@event == null)
                return NotFound($"Evento {id} no encontrado");

            return Ok(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener evento {EventId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Crear un nuevo evento
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EventDto>> CreateEvent([FromBody] CreateEventRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("El nombre del evento es requerido");

            if (request.StartsAt == default)
                return BadRequest("La fecha de inicio es requerida");

            var @event = await _eventService.CreateEventAsync(request);
            _logger.LogInformation("Evento creado: {EventId}", @event.Id);
            return CreatedAtAction(nameof(GetEvent), new { id = @event.Id }, @event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear evento");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Actualizar un evento
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<EventDto>> UpdateEvent(long id, [FromBody] UpdateEventRequest request)
    {
        try
        {
            var updated = await _eventService.UpdateEventAsync(id, request);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Evento {id} no encontrado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar evento {EventId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Eliminar un evento
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(long id)
    {
        try
        {
            var deleted = await _eventService.DeleteEventAsync(id);
            if (!deleted)
                return NotFound($"Evento {id} no encontrado");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar evento {EventId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
