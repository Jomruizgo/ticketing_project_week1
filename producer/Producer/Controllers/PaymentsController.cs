using Microsoft.AspNetCore.Mvc;
using Producer.Models;
using Producer.Services;

namespace Producer.Controllers;

/// <summary>
/// Controlador para gestionar operaciones de pago de tickets
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentPublisher _paymentPublisher;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentPublisher paymentPublisher,
        ILogger<PaymentsController> logger)
    {
        _paymentPublisher = paymentPublisher;
        _logger = logger;
    }

    /// <summary>
    /// Procesa un pago para un ticket
    /// Simula la validación del pago y publica los eventos correspondientes
    /// </summary>
    /// <param name="request">Datos del pago</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>202 Accepted - El pago ha sido encolado para procesamiento</returns>
    [HttpPost("process")]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] ProcessPaymentRequest request,
        CancellationToken cancellationToken)
    {
        // Validar entrada
        if (request == null)
        {
            return BadRequest(new { message = "La solicitud no puede estar vacía" });
        }

        if (request.TicketId <= 0)
        {
            return BadRequest(new { message = "TicketId debe ser mayor a 0" });
        }

        if (request.EventId <= 0)
        {
            return BadRequest(new { message = "EventId debe ser mayor a 0" });
        }

        if (request.AmountCents <= 0)
        {
            return BadRequest(new { message = "AmountCents debe ser mayor a 0" });
        }

        if (string.IsNullOrWhiteSpace(request.PaymentBy))
        {
            return BadRequest(new { message = "PaymentBy es requerido" });
        }

        if (string.IsNullOrWhiteSpace(request.PaymentMethodId))
        {
            return BadRequest(new { message = "PaymentMethodId es requerido" });
        }

        try
        {
            // Simular validación y procesamiento de pago
            // En producción, aquí se integraría con un gateway de pago (Stripe, PayPal, etc)
            var isApproved = await SimulatePaymentProcessing(request, cancellationToken);

            if (isApproved)
            {
                // Crear evento de pago aprobado
                var approvedEvent = new PaymentApprovedEvent
                {
                    TicketId = request.TicketId,
                    EventId = request.EventId,
                    AmountCents = request.AmountCents,
                    Currency = request.Currency,
                    PaymentBy = request.PaymentBy,
                    TransactionRef = request.TransactionRef ?? $"TXN-{Guid.NewGuid()}",
                    ApprovedAt = DateTime.UtcNow
                };

                // Publicar evento de pago aprobado
                await _paymentPublisher.PublishPaymentApprovedAsync(approvedEvent, cancellationToken);

                _logger.LogInformation(
                    "Pago aprobado publicado: TicketId={TicketId}, EventId={EventId}, Amount={Amount}{Currency}",
                    request.TicketId,
                    request.EventId,
                    request.AmountCents / 100.0,
                    request.Currency);
            }
            else
            {
                // Crear evento de pago rechazado
                var rejectedEvent = new PaymentRejectedEvent
                {
                    TicketId = request.TicketId,
                    EventId = request.EventId,
                    AmountCents = request.AmountCents,
                    Currency = request.Currency,
                    PaymentBy = request.PaymentBy,
                    TransactionRef = request.TransactionRef,
                    RejectionReason = "Fondos insuficientes o tarjeta rechazada",
                    RejectedAt = DateTime.UtcNow
                };

                // Publicar evento de pago rechazado
                await _paymentPublisher.PublishPaymentRejectedAsync(rejectedEvent, cancellationToken);

                _logger.LogWarning(
                    "Pago rechazado publicado: TicketId={TicketId}, EventId={EventId}",
                    request.TicketId,
                    request.EventId);
            }

            // Retornar 202 Accepted - el procesamiento es asincrónico
            return Accepted(new
            {
                message = "Pago encolado para procesamiento",
                ticketId = request.TicketId,
                eventId = request.EventId,
                status = isApproved ? "approved" : "rejected"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al procesar pago: TicketId={TicketId}, EventId={EventId}",
                request.TicketId,
                request.EventId);

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Error al procesar el pago",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Simula el procesamiento de un pago
    /// En producción, se integraría con un servicio de pago real
    /// </summary>
    private async Task<bool> SimulatePaymentProcessing(ProcessPaymentRequest request, CancellationToken cancellationToken)
    {
        // Simular latencia de procesamiento
        await Task.Delay(Random.Shared.Next(100, 500), cancellationToken);

        // Simular 80% de probabilidad de éxito
        var randomValue = Random.Shared.Next(0, 100);
        return randomValue < 80;
    }
}
