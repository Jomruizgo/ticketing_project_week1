using Microsoft.Extensions.Logging;
using MsPaymentService.Application.Dtos;
using MsPaymentService.Application.Interfaces;
using MsPaymentService.Domain.Entities;
using MsPaymentService.Domain.Interfaces;

namespace MsPaymentService.Application.UseCases.ProcessRejectedPayment;

public class ProcessRejectedPaymentCommandHandler
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketStateService _stateService;
    private readonly ILogger<ProcessRejectedPaymentCommandHandler> _logger;

    public ProcessRejectedPaymentCommandHandler(
        ITicketRepository ticketRepository,
        ITicketStateService stateService,
        ILogger<ProcessRejectedPaymentCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _stateService = stateService;
        _logger = logger;
    }

    public async Task<ValidationResult> HandleAsync(ProcessRejectedPaymentCommand command)
    {
        try
        {
            var ticket = await _ticketRepository.GetByIdAsync(command.TicketId);

            if (ticket == null)
            {
                _logger.LogWarning("Ticket {TicketId} not found", command.TicketId);
                return ValidationResult.Failure("Ticket not found");
            }

            if (ticket.Status == TicketStatus.released)
            {
                _logger.LogInformation(
                    "Ticket {TicketId} already released. Skipping duplicate event",
                    command.TicketId);
                return ValidationResult.AlreadyProcessed();
            }

            var success = await _stateService.TransitionToReleasedAsync(
                command.TicketId,
                $"Payment rejected: {command.RejectionReason}");

            if (success)
            {
                _logger.LogInformation("Payment rejection processed for ticket {TicketId}", command.TicketId);
                return ValidationResult.Success();
            }

            return ValidationResult.Failure("Failed to transition ticket to released status");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing rejected payment for ticket {TicketId}", command.TicketId);
            throw;
        }
    }
}
