using Microsoft.Extensions.Logging;
using MsPaymentService.Application.Dtos;
using MsPaymentService.Application.Interfaces;
using MsPaymentService.Domain.Entities;
using MsPaymentService.Domain.Interfaces;

namespace MsPaymentService.Application.UseCases.ProcessApprovedPayment;

public class ProcessApprovedPaymentCommandHandler
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITicketStateService _stateService;
    private readonly ILogger<ProcessApprovedPaymentCommandHandler> _logger;

    public ProcessApprovedPaymentCommandHandler(
        ITicketRepository ticketRepository,
        IPaymentRepository paymentRepository,
        ITicketStateService stateService,
        ILogger<ProcessApprovedPaymentCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _paymentRepository = paymentRepository;
        _stateService = stateService;
        _logger = logger;
    }

    public async Task<ValidationResult> HandleAsync(ProcessApprovedPaymentCommand command)
    {
        try
        {
            var ticket = await _ticketRepository.GetByIdAsync(command.TicketId);

            if (ticket == null)
            {
                _logger.LogWarning("Ticket {TicketId} not found", command.TicketId);
                return ValidationResult.Failure("Ticket not found");
            }

            if (ticket.Status == TicketStatus.paid)
            {
                _logger.LogInformation("Ticket {TicketId} already paid. Skipping duplicate event", command.TicketId);
                return ValidationResult.AlreadyProcessed();
            }

            if (ticket.Status != TicketStatus.reserved)
            {
                _logger.LogWarning(
                    "Invalid ticket status for payment. TicketId: {TicketId}, Status: {Status}",
                    command.TicketId, ticket.Status);
                return ValidationResult.Failure($"Invalid ticket status: {ticket.Status}");
            }

            if (ticket.ReservedAt == null || !IsWithinTimeLimit(ticket.ReservedAt.Value, command.ApprovedAt))
            {
                _logger.LogWarning(
                    "Payment received after TTL. TicketId: {TicketId}, ReservedAt: {ReservedAt}, ApprovedAt: {ApprovedAt}",
                    command.TicketId, ticket.ReservedAt, command.ApprovedAt);

                await _stateService.TransitionToReleasedAsync(command.TicketId, "Payment received after TTL");
                return ValidationResult.Failure("TTL exceeded");
            }

            var payment = await _paymentRepository.GetByTicketIdAsync(command.TicketId);
            if (payment == null)
            {
                payment = await _paymentRepository.CreateAsync(new Payment
                {
                    TicketId = command.TicketId,
                    Status = PaymentStatus.pending,
                    AmountCents = command.AmountCents,
                    Currency = command.Currency,
                    ProviderRef = command.TransactionRef
                });
            }

            var success = await _stateService.TransitionToPaidAsync(command.TicketId, command.TransactionRef);

            if (success)
            {
                _logger.LogInformation("Payment processed successfully for ticket {TicketId}", command.TicketId);
                return ValidationResult.Success();
            }

            return ValidationResult.Failure("Failed to transition ticket to paid status");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing approved payment for ticket {TicketId}", command.TicketId);
            throw;
        }
    }

    public bool IsWithinTimeLimit(DateTime reservedAt, DateTime paymentReceivedAt)
    {
        var expirationTime = reservedAt.AddMinutes(5);
        return paymentReceivedAt <= expirationTime;
    }
}
