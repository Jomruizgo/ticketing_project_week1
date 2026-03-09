using Microsoft.AspNetCore.Mvc;
using Producer.Api.Models;
using Producer.Application.DTOs.ReserveTicket;
using Producer.Application.Interfaces;

namespace Producer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly IReserveTicketUseCase _handler;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(
        IReserveTicketUseCase handler,
        ILogger<TicketsController> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    [HttpPost("reserve")]
    public async Task<IActionResult> ReserveTicket(
        [FromBody] ReserveTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest("La solicitud no puede estar vacía");
        }

        if (request.EventId <= 0)
        {
            return BadRequest("EventId debe ser mayor a 0");
        }

        if (request.TicketId <= 0)
        {
            return BadRequest("TicketId debe ser mayor a 0");
        }

        if (string.IsNullOrWhiteSpace(request.OrderId))
        {
            return BadRequest("OrderId es requerido");
        }

        if (string.IsNullOrWhiteSpace(request.ReservedBy))
        {
            return BadRequest("ReservedBy es requerido");
        }

        if (request.ExpiresInSeconds <= 0)
        {
            return BadRequest("ExpiresInSeconds debe ser mayor a 0");
        }

        try
        {
            var command = new ReserveTicketCommand(
                request.EventId,
                request.TicketId,
                request.OrderId,
                request.ReservedBy,
                request.ExpiresInSeconds);

            var response = await _handler.HandleAsync(command, cancellationToken);

            _logger.LogInformation(
                "Ticket reservado exitosamente. TicketId: {TicketId}, OrderId: {OrderId}",
                request.TicketId,
                request.OrderId);

            return Accepted(new { message = response.Message, ticketId = response.TicketId });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al reservar ticket. TicketId: {TicketId}",
                request.TicketId);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "Error al procesar la reserva" });
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
