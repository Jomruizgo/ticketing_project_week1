using Microsoft.Extensions.Logging;
using ReservationService.Application.UseCases.ProcessExpiration;
using ReservationService.Application.Interfaces;
using ReservationService.Domain.Entities;
using ReservationService.Domain.Interfaces;

namespace ReservationService.Application.UseCases.ProcessExpiration;

public class ProcessExpirationCommandHandler : IProcessExpirationUseCase
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<ProcessExpirationCommandHandler> _logger;

    public ProcessExpirationCommandHandler(
        ITicketRepository ticketRepository,
        ILogger<ProcessExpirationCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _logger = logger;
    }

    public async Task<ProcessExpirationResponse> HandleAsync(
        ProcessExpirationCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing expiration for Ticket {TicketId}", command.TicketId);

            var ticket = await _ticketRepository.GetByIdAsync(command.TicketId, cancellationToken);

            if (ticket is null)
            {
                _logger.LogWarning("Ticket {TicketId} not found during expiration", command.TicketId);
                return new ProcessExpirationResponse(true, false);
            }

            if (ticket.Status is TicketStatus.Paid or TicketStatus.Released)
            {
                _logger.LogInformation(
                    "Ticket {TicketId} in final status {Status}. Expiration ignored.",
                    command.TicketId,
                    ticket.Status);
                return new ProcessExpirationResponse(true, false);
            }

            if (ticket.Status != TicketStatus.Reserved)
            {
                _logger.LogInformation(
                    "Ticket {TicketId} in status {Status}. Expiration treated as idempotent no-op.",
                    command.TicketId,
                    ticket.Status);
                return new ProcessExpirationResponse(true, false);
            }

            var released = await _ticketRepository.TryReleaseAsync(ticket, cancellationToken);

            if (!released)
            {
                return new ProcessExpirationResponse(false, false, "Ticket was modified by another process");
            }

            _logger.LogInformation("Ticket {TicketId} released due to expiration", command.TicketId);
            return new ProcessExpirationResponse(true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Technical error processing expiration for ticket {TicketId}", command.TicketId);
            return new ProcessExpirationResponse(false, false, "Technical error while processing expiration");
        }
    }
}
