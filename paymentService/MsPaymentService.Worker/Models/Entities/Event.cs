namespace MsPaymentService.Worker.Models.Entities;

/// <summary>
/// Representa un evento (concierto, espectáculo, etc.) al cual se asocian tickets.
/// </summary>
public class Event
{
    /// <summary>Identificador único del evento.</summary>
    public long Id { get; set; }

    /// <summary>Nombre descriptivo del evento.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Fecha y hora de inicio del evento (UTC).</summary>
    public DateTime StartsAt { get; set; }

    /// <summary>Colección de tickets asociados a este evento.</summary>
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}