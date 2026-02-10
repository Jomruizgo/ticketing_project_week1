using MsPaymentService.Worker.Models.DTOs;
using MsPaymentService.Worker.Models.Events;

namespace MsPaymentService.Worker.Services;

public interface IPaymentValidationService
{
    Task<ValidationResult> ValidateAndProcessApprovedPaymentAsync(PaymentApprovedEvent paymentEvent);
    Task<ValidationResult> ValidateAndProcessRejectedPaymentAsync(PaymentRejectedEvent paymentEvent);
    bool IsWithinTimeLimit(DateTime reservedAt, DateTime paymentReceivedAt);
}