using MsPaymentService.Worker.Models.DTOs;
using MsPaymentService.Worker.Models.Entities;
using MsPaymentService.Worker.Models.Events;
using MsPaymentService.Worker.Repositories;

namespace MsPaymentService.Worker.Services;

public class PaymentValidationService : IPaymentValidationService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITicketStateService _stateService;
    private readonly ILogger<PaymentValidationService> _logger;

    public PaymentValidationService(
        ITicketRepository ticketRepository,
        IPaymentRepository paymentRepository,
        ITicketStateService stateService,
        ILogger<PaymentValidationService> logger)
    {
        _ticketRepository = ticketRepository;
        _paymentRepository = paymentRepository;
        _stateService = stateService;
        _logger = logger;
    }

    public async Task<ValidationResult> ValidateAndProcessApprovedPaymentAsync(PaymentApprovedEvent paymentEvent)
    {
        try
        {
            // 1. Verificar idempotencia
            var ticket = await _ticketRepository.GetByIdAsync(paymentEvent.TicketId);
            
            if (ticket == null)
            {
                _logger.LogWarning("Ticket {TicketId} not found", paymentEvent.TicketId);
                return ValidationResult.Failure("Ticket not found");
            }

            if (ticket.Status == TicketStatus.paid)
            {
                _logger.LogInformation(
                    "Ticket {TicketId} already paid. Skipping duplicate event",
                    paymentEvent.TicketId);
                return ValidationResult.AlreadyProcessed();
            }

            // 2. Validar estado actual
            if (ticket.Status != TicketStatus.reserved)
            {
                _logger.LogWarning(
                    "Invalid ticket status for payment. TicketId: {TicketId}, Status: {Status}",
                    paymentEvent.TicketId, ticket.Status);
                return ValidationResult.Failure($"Invalid ticket status: {ticket.Status}");
            }

            // 3. Validar TTL
            if (ticket.ReservedAt == null || !IsWithinTimeLimit(ticket.ReservedAt.Value, paymentEvent.PublishedAt))
            {
                _logger.LogWarning(
                    "Payment received after TTL. TicketId: {TicketId}, ReservedAt: {ReservedAt}, PublishedAt: {PublishedAt}",
                    paymentEvent.TicketId, ticket.ReservedAt, paymentEvent.PublishedAt);
                    
                // Marcar ticket como released por TTL expirado
                await _stateService.TransitionToReleasedAsync(paymentEvent.TicketId, "Payment received after TTL");
                return ValidationResult.Failure("TTL exceeded");
            }

            // 4. Validar payment
            var payment = await _paymentRepository.GetByTicketIdAsync(paymentEvent.TicketId);
            if (payment == null || payment.Status != PaymentStatus.pending)
            {
                _logger.LogWarning(
                    "Invalid payment status. TicketId: {TicketId}, PaymentStatus: {PaymentStatus}",
                    paymentEvent.TicketId, payment?.Status);
                return ValidationResult.Failure("Invalid payment status");
            }

            // 5. Procesar transición exitosa
            var success = await _stateService.TransitionToPaidAsync(paymentEvent.TicketId, paymentEvent.OrderId);
            
            if (success)
            {
                _logger.LogInformation("Payment processed successfully for ticket {TicketId}", paymentEvent.TicketId);
                return ValidationResult.Success();
            }
            
            return ValidationResult.Failure("Failed to transition ticket to paid status");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing approved payment for ticket {TicketId}", paymentEvent.TicketId);
            throw;
        }
    }

    public async Task<ValidationResult> ValidateAndProcessRejectedPaymentAsync(PaymentRejectedEvent paymentEvent)
    {
        try
        {
            var ticket = await _ticketRepository.GetByIdAsync(paymentEvent.TicketId);
            
            if (ticket == null)
            {
                _logger.LogWarning("Ticket {TicketId} not found", paymentEvent.TicketId);
                return ValidationResult.Failure("Ticket not found");
            }

            // Si ya está released, no hacer nada (idempotencia)
            if (ticket.Status == TicketStatus.released)
            {
                _logger.LogInformation(
                    "Ticket {TicketId} already released. Skipping duplicate event",
                    paymentEvent.TicketId);
                return ValidationResult.AlreadyProcessed();
            }

            // Procesar rechazo
            var success = await _stateService.TransitionToReleasedAsync(
                paymentEvent.TicketId, 
                $"Payment rejected: {paymentEvent.RejectionReason}");
                
            if (success)
            {
                _logger.LogInformation("Payment rejection processed for ticket {TicketId}", paymentEvent.TicketId);
                return ValidationResult.Success();
            }
            
            return ValidationResult.Failure("Failed to transition ticket to released status");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing rejected payment for ticket {TicketId}", paymentEvent.TicketId);
            throw;
        }
    }

    public bool IsWithinTimeLimit(DateTime reservedAt, DateTime paymentReceivedAt)
    {
        var expirationTime = reservedAt.AddMinutes(5);
        return paymentReceivedAt <= expirationTime;
    }
}