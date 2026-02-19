namespace Producer.Application.UseCases.RequestPayment;

public record RequestPaymentCommand(
    int TicketId,
    int EventId,
    int AmountCents,
    string Currency,
    string PaymentBy,
    string PaymentMethodId,
    string? TransactionRef);
