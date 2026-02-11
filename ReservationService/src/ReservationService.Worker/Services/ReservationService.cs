using ReservationService.Worker.Models;
using ReservationService.Worker.Repositories;

namespace ReservationService.Worker.Services;

public class ReservationServiceImpl : IReservationService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<ReservationServiceImpl> _logger;

    public ReservationServiceImpl(ITicketRepository ticketRepository, ILogger<ReservationServiceImpl> logger)
    {
        _ticketRepository = ticketRepository;
        _logger = logger;
    }

    // 🛡 HUMAN CHECK:
    // La lógica de reserva valida primero que el ticket exista y esté disponible.
    // Si ya fue reservado por otro proceso, se rechaza silenciosamente
    // (no es un error, es un escenario esperado en alta concurrencia).
    public async Task<ReservationResult> ProcessReservationAsync(
        ReservationMessage message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing reservation for Ticket {TicketId}, Order {OrderId}, User {ReservedBy}",
            message.TicketId, message.OrderId, message.ReservedBy);

        // 1. Buscar el ticket
        var ticket = await _ticketRepository.GetByIdAsync(message.TicketId, cancellationToken);

        if (ticket is null)
        {
            _logger.LogWarning("Ticket {TicketId} not found", message.TicketId);
            return new ReservationResult(false, $"Ticket {message.TicketId} not found");
        }

        // 2. Validar que esté disponible
        if (ticket.Status != TicketStatus.Available)
        {
            _logger.LogWarning(
                "Ticket {TicketId} is not available. Current status: {Status}",
                message.TicketId, ticket.Status);
            return new ReservationResult(false, $"Ticket {message.TicketId} is not available (status: {ticket.Status})");
        }

        // 3. Calcular tiempo de expiración
        var expiresAt = DateTime.UtcNow.AddSeconds(message.ReservationDurationSeconds);

        // 4. Intentar reservar (con optimistic locking)
        var reserved = await _ticketRepository.TryReserveAsync(
            ticket,
            message.ReservedBy,
            message.OrderId,
            expiresAt,
            cancellationToken);

        if (!reserved)
        {
            return new ReservationResult(false, "Ticket was modified by another process");
        }

        _logger.LogInformation(
            "Reservation completed for Ticket {TicketId}. Expires at {ExpiresAt}",
            message.TicketId, expiresAt);

        return new ReservationResult(true);
    }
}
