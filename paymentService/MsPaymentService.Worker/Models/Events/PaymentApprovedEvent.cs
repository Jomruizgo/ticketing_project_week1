namespace MsPaymentService.Worker.Models.Events;

public class PaymentApprovedEvent
{
    public long TicketId { get; set; }
    public long EventId { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string ReservedBy { get; set; } = string.Empty;
    public int ReservationDurationSeconds { get; set; }
    public DateTime PublishedAt { get; set; }
}