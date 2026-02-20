namespace CrudService.Domain.Entities;

public class Event
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime StartsAt { get; set; }
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
