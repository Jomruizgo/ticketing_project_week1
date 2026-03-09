namespace MsPaymentService.Application.UseCases.ProcessRejectedPayment;

public record ProcessRejectedPaymentCommand(
    long TicketId,
    long PaymentId,
    string? ProviderReference,
    string RejectionReason,
    DateTime RejectedAt,
    long EventId);
