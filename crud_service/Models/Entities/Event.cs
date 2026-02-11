namespace CrudService.Models.Entities;

/// <summary>
/// Representa un evento en el sistema
/// </summary>
public class Event
{
    /// <summary>
    /// ID único del evento
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Nombre del evento
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Fecha y hora de inicio del evento
    /// </summary>
    public DateTime StartsAt { get; set; }

    /// <summary>
    /// Tickets asociados al evento
    /// </summary>
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
