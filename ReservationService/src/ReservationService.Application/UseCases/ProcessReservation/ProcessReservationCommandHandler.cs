using Microsoft.Extensions.Logging;
using ReservationService.Domain.Entities;
using ReservationService.Domain.Interfaces;

namespace ReservationService.Application.UseCases.ProcessReservation;

public class ProcessReservationCommandHandler
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<ProcessReservationCommandHandler> _logger;

    public ProcessReservationCommandHandler(
        ITicketRepository ticketRepository,
        ILogger<ProcessReservationCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _logger = logger;
    }

    public async Task<ProcessReservationResponse> HandleAsync(
        ProcessReservationCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing reservation for Ticket {TicketId}, Order {OrderId}, User {ReservedBy}",
            command.TicketId, command.OrderId, command.ReservedBy);

        var ticket = await _ticketRepository.GetByIdAsync(command.TicketId, cancellationToken);

        if (ticket is null)
        {
            _logger.LogWarning("Ticket {TicketId} not found", command.TicketId);
            return new ProcessReservationResponse(false, $"Ticket {command.TicketId} not found");
        }

        if (ticket.Status != TicketStatus.Available)
        {
            _logger.LogWarning(
                "Ticket {TicketId} is not available. Current status: {Status}",
                command.TicketId, ticket.Status);
            return new ProcessReservationResponse(false, $"Ticket {command.TicketId} is not available (status: {ticket.Status})");
        }

        var expiresAt = DateTime.UtcNow.AddSeconds(command.ReservationDurationSeconds);

        var reserved = await _ticketRepository.TryReserveAsync(
            ticket,
            command.ReservedBy,
            command.OrderId,
            expiresAt,
            cancellationToken);

        if (!reserved)
        {
            return new ProcessReservationResponse(false, "Ticket was modified by another process");
        }

        _logger.LogInformation(
            "Reservation completed for Ticket {TicketId}. Expires at {ExpiresAt}",
            command.TicketId, expiresAt);

        return new ProcessReservationResponse(true);
    }
}
