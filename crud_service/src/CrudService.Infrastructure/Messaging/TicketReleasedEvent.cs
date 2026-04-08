namespace CrudService.Infrastructure.Messaging;

public class TicketReleasedEvent
{
    public long TicketId { get; set; }
    public long EventId { get; set; }
    public DateTime ReleasedAt { get; set; }
}
