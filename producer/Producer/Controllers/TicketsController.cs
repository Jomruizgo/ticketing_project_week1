using Microsoft.AspNetCore.Mvc;
using Producer.Models;
using Producer.Services;

namespace Producer.Controllers;

/// <summary>
/// Controlador para gestionar operaciones relacionadas con tickets
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly ITicketPublisher _publisher;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(
        ITicketPublisher publisher,
        ILogger<TicketsController> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// Reserva un ticket y publica el evento a RabbitMQ
    /// </summary>
    /// <param name="request">Datos de la reserva</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Respuesta con el estado de la operación</returns>
    [HttpPost("reserve")]
    public async Task<IActionResult> ReserveTicket(
        [FromBody] ReserveTicketRequest request,
        CancellationToken cancellationToken)
    {
        // Validar entrada
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
            // Crear el evento
            var ticketEvent = new TicketReservedEvent
            {
                TicketId = request.TicketId,
                EventId = request.EventId,
                OrderId = request.OrderId,
                ReservedBy = request.ReservedBy,
                ReservationDurationSeconds = request.ExpiresInSeconds,
                PublishedAt = DateTime.UtcNow
            };

            // Publicar a RabbitMQ
            await _publisher.PublishTicketReservedAsync(ticketEvent, cancellationToken);

            _logger.LogInformation(
                "Ticket reservado exitosamente. TicketId: {TicketId}, OrderId: {OrderId}",
                request.TicketId,
                request.OrderId);

            return Accepted(new { message = "Reserva procesada", ticketId = request.TicketId });
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

    /// <summary>
    /// Health check
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
