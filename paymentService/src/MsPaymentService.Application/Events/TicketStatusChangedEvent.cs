namespace MsPaymentService.Application.Events;

public class TicketStatusChangedEvent
{
    public int TicketId { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}
