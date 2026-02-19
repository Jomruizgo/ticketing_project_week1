namespace Producer.Api.Models;

public class ProcessPaymentRequest
{
    public int TicketId { get; set; }
    public int EventId { get; set; }
    public int AmountCents { get; set; }
    public string Currency { get; set; } = "USD";
    public required string PaymentBy { get; set; }
    public required string PaymentMethodId { get; set; }
    public string? TransactionRef { get; set; }
}
