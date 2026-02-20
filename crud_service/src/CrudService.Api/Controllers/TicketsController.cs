using System.Text.Json;
using CrudService.Application.Dtos;
using CrudService.Application.Exceptions;
using CrudService.Application.UseCases.Tickets.CreateTickets;
using CrudService.Application.UseCases.Tickets.GetExpiredTickets;
using CrudService.Application.UseCases.Tickets.GetTicketById;
using CrudService.Application.UseCases.Tickets.GetTicketsByEvent;
using CrudService.Application.UseCases.Tickets.ReleaseTicket;
using CrudService.Application.UseCases.Tickets.UpdateTicketStatus;
using CrudService.Domain.Exceptions;
using CrudService.Infrastructure.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace CrudService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly GetTicketsByEventQueryHandler _getTicketsByEventHandler;
    private readonly GetTicketByIdQueryHandler _getTicketByIdHandler;
    private readonly CreateTicketsCommandHandler _createTicketsHandler;
    private readonly UpdateTicketStatusCommandHandler _updateTicketStatusHandler;
    private readonly ReleaseTicketCommandHandler _releaseTicketHandler;
    private readonly GetExpiredTicketsQueryHandler _getExpiredTicketsHandler;
    private readonly TicketStatusHub _statusHub;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(
        GetTicketsByEventQueryHandler getTicketsByEventHandler,
        GetTicketByIdQueryHandler getTicketByIdHandler,
        CreateTicketsCommandHandler createTicketsHandler,
        UpdateTicketStatusCommandHandler updateTicketStatusHandler,
        ReleaseTicketCommandHandler releaseTicketHandler,
        GetExpiredTicketsQueryHandler getExpiredTicketsHandler,
        TicketStatusHub statusHub,
        ILogger<TicketsController> logger)
    {
        _getTicketsByEventHandler = getTicketsByEventHandler;
        _getTicketByIdHandler = getTicketByIdHandler;
        _createTicketsHandler = createTicketsHandler;
        _updateTicketStatusHandler = updateTicketStatusHandler;
        _releaseTicketHandler = releaseTicketHandler;
        _getExpiredTicketsHandler = getExpiredTicketsHandler;
        _statusHub = statusHub;
        _logger = logger;
    }

    /// <summary>
    /// SSE stream: emite un evento cuando el ticket cambia de estado y cierra la conexión.
    /// </summary>
    [HttpGet("{id}/stream")]
    public async Task StreamStatus(long id, CancellationToken clientDisconnected)
    {
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var combined = CancellationTokenSource.CreateLinkedTokenSource(clientDisconnected, timeout.Token);

        var reader = _statusHub.Subscribe(id);

        try
        {
            await foreach (var update in reader.ReadAllAsync(combined.Token))
            {
                var data = JsonSerializer.Serialize(new { ticketId = update.TicketId, status = update.NewStatus });
                await Response.WriteAsync($"data: {data}\n\n", combined.Token);
                await Response.Body.FlushAsync(combined.Token);
                break;
            }
        }
        catch (OperationCanceledException) { }
    }

    [HttpGet("event/{eventId}")]
    public async Task<ActionResult<IEnumerable<TicketDto>>> GetTicketsByEvent(long eventId)
    {
        try
        {
            var tickets = await _getTicketsByEventHandler.HandleAsync(new GetTicketsByEventQuery(eventId));
            return Ok(tickets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tickets del evento {EventId}", eventId);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TicketDto>> GetTicket(long id)
    {
        try
        {
            var ticket = await _getTicketByIdHandler.HandleAsync(new GetTicketByIdQuery(id));
            if (ticket == null) return NotFound($"Ticket {id} no encontrado");
            return Ok(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener ticket {TicketId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<IEnumerable<TicketDto>>> CreateTickets([FromBody] CreateTicketsRequest request)
    {
        try
        {
            if (request.EventId <= 0) return BadRequest("EventId debe ser mayor a 0");
            if (request.Quantity <= 0 || request.Quantity > 1000) return BadRequest("Quantity debe estar entre 1 y 1000");

            var tickets = await _createTicketsHandler.HandleAsync(new CreateTicketsCommand(request.EventId, request.Quantity));
            _logger.LogInformation("Creados {Quantity} tickets para evento {EventId}", request.Quantity, request.EventId);
            return CreatedAtAction(nameof(GetTicketsByEvent), new { eventId = request.EventId }, tickets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tickets");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<TicketDto>> UpdateTicketStatus(long id, [FromBody] UpdateTicketStatusRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.NewStatus)) return BadRequest("NewStatus es requerido");

            var ticket = await _updateTicketStatusHandler.HandleAsync(
                new UpdateTicketStatusCommand(id, request.NewStatus, request.Reason));
            return Ok(ticket);
        }
        catch (TicketNotFoundException)
        {
            return NotFound($"Ticket {id} no encontrado");
        }
        catch (InvalidTicketStatusException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar status del ticket {TicketId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpDelete("{id}/release")]
    public async Task<ActionResult<TicketDto>> ReleaseTicket(long id, [FromQuery] string? reason = null)
    {
        try
        {
            var ticket = await _releaseTicketHandler.HandleAsync(new ReleaseTicketCommand(id, reason));
            _logger.LogInformation("Ticket {TicketId} liberado. Razón: {Reason}", id, reason ?? "No especificada");
            return Ok(ticket);
        }
        catch (TicketNotFoundException)
        {
            return NotFound($"Ticket {id} no encontrado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al liberar ticket {TicketId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("expired/list")]
    public async Task<ActionResult<IEnumerable<TicketDto>>> GetExpiredTickets()
    {
        try
        {
            var expiredTickets = await _getExpiredTicketsHandler.HandleAsync(new GetExpiredTicketsQuery());
            return Ok(expiredTickets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tickets expirados");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
}

public class CreateTicketsRequest
{
    public long EventId { get; set; }
    public int Quantity { get; set; }
}
