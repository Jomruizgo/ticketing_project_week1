namespace CrudService.Models.Entities;

/// <summary>
/// Representa un registro del historial de cambios de un ticket
/// </summary>
public class TicketHistory
{
    /// <summary>
    /// ID único del registro
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// ID del ticket
    /// </summary>
    public long TicketId { get; set; }

    /// <summary>
    /// Estado anterior
    /// </summary>
    public TicketStatus OldStatus { get; set; }

    /// <summary>
    /// Estado nuevo
    /// </summary>
    public TicketStatus NewStatus { get; set; }

    /// <summary>
    /// Fecha del cambio
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Razón del cambio
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Ticket asociado
    /// </summary>
    public Ticket Ticket { get; set; } = null!;
}
