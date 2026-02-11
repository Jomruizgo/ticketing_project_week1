namespace MsPaymentService.Worker.Models.Entities;

/// <summary>
/// Registro de auditoría que almacena cada cambio de estado de un ticket.
/// Permite trazabilidad completa del ciclo de vida del ticket.
/// </summary>
public class TicketHistory
{
    /// <summary>Identificador único del registro de historial.</summary>
    public long Id { get; set; }

    /// <summary>Identificador del ticket al que pertenece este registro.</summary>
    public long TicketId { get; set; }

    /// <summary>Estado anterior del ticket antes de la transición.</summary>
    public TicketStatus OldStatus { get; set; }

    /// <summary>Nuevo estado del ticket después de la transición.</summary>
    public TicketStatus NewStatus { get; set; }

    /// <summary>Fecha y hora en que se realizó el cambio de estado (UTC).</summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>Motivo o descripción del cambio de estado.</summary>
    public string? Reason { get; set; }

    /// <summary>Ticket asociado a este registro de historial.</summary>
    public Ticket Ticket { get; set; } = null!;
}