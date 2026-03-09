namespace MsPaymentService.Application.UseCases.ProcessApprovedPayment;

public record ProcessApprovedPaymentCommand(
    long TicketId,
    long EventId,
    int AmountCents,
    string Currency,
    string PaymentBy,
    string TransactionRef,
    DateTime ApprovedAt);
