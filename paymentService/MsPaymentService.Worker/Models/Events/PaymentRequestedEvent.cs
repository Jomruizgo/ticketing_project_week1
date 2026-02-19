namespace MsPaymentService.Worker.Models.Events;

public class PaymentRequestedEvent
{
    public int TicketId { get; set; }
    public int EventId { get; set; }
    public int AmountCents { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentBy { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = string.Empty;
    public string? TransactionRef { get; set; }
    public DateTime RequestedAt { get; set; }
}
