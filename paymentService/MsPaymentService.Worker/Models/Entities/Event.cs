namespace MsPaymentService.Worker.Models.Entities;

public class Event
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    
    // Navigation properties
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}