namespace MsPaymentService.Application.Events;

public class PaymentApprovedEvent
{
    public long TicketId { get; set; }
    public long EventId { get; set; }
    public int AmountCents { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentBy { get; set; } = string.Empty;
    public string TransactionRef { get; set; } = string.Empty;
    public DateTime ApprovedAt { get; set; }
}
