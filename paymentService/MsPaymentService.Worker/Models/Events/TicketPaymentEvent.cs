namespace MsPaymentService.Worker.Models.Events;

public class TicketPaymentEvent
{
    public long TicketId { get; set; }
    public long EventId { get; set; }
    public string OrderId { get; set; } = default!;
    public string ReservedBy { get; set; } = default!;
    public int ReservationDurationSeconds { get; set; }
    public DateTime PublishedAt { get; set; }
}
