namespace CrudService.Application.Dtos;

public class EventDto
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime StartsAt { get; set; }
    public int AvailableTickets { get; set; }
    public int ReservedTickets { get; set; }
    public int PaidTickets { get; set; }
}

public class CreateEventRequest
{
    public string Name { get; set; } = null!;
    public DateTime StartsAt { get; set; }
}

public class UpdateEventRequest
{
    public string? Name { get; set; }
    public DateTime? StartsAt { get; set; }
}
