namespace CrudService.Models.DTOs;

/// <summary>
/// DTO para respuesta de evento
/// </summary>
public class EventDto
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime StartsAt { get; set; }
    public int AvailableTickets { get; set; }
    public int ReservedTickets { get; set; }
    public int PaidTickets { get; set; }
}

/// <summary>
/// DTO para crear un evento
/// </summary>
public class CreateEventRequest
{
    public string Name { get; set; } = null!;
    public DateTime StartsAt { get; set; }
}

/// <summary>
/// DTO para actualizar un evento
/// </summary>
public class UpdateEventRequest
{
    public string? Name { get; set; }
    public DateTime? StartsAt { get; set; }
}
