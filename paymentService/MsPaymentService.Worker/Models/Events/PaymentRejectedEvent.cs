namespace MsPaymentService.Worker.Models.Events;

public class PaymentRejectedEvent
{
    public long TicketId { get; set; }
    public long PaymentId { get; set; }
    public string? ProviderReference { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
    public DateTime RejectedAt { get; set; }
    
    // Metadatos básicos
    public string EventId { get; set; } = string.Empty;
    public DateTime EventTimestamp { get; set; }
}