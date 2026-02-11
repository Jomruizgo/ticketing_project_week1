namespace MsPaymentService.Worker.Models.Entities;

/// <summary>
/// Estados posibles de un ticket en su ciclo de vida.
/// Los valores están en minúsculas para coincidir con los enums nativos de PostgreSQL.
/// </summary>
public enum TicketStatus
{
    /// <summary>Ticket disponible para reserva.</summary>
    available,
    /// <summary>Ticket reservado temporalmente por un usuario.</summary>
    reserved,
    /// <summary>Ticket con pago confirmado.</summary>
    paid,
    /// <summary>Ticket liberado tras expiración de reserva o rechazo de pago.</summary>
    released,
    /// <summary>Ticket cancelado manualmente.</summary>
    cancelled
}

/// <summary>
/// Representa un ticket asociado a un evento.
/// Implementa control de concurrencia optimista mediante el campo <see cref="Version"/>.
/// </summary>
public class Ticket
{
    /// <summary>Identificador único del ticket.</summary>
    public long Id { get; set; }

    /// <summary>Identificador del evento al que pertenece el ticket.</summary>
    public long EventId { get; set; }

    /// <summary>Estado actual del ticket en su ciclo de vida.</summary>
    public TicketStatus Status { get; set; }

    /// <summary>Fecha y hora en que se realizó la reserva (UTC). Null si no está reservado.</summary>
    public DateTime? ReservedAt { get; set; }

    /// <summary>Fecha y hora de expiración de la reserva (UTC). Null si no está reservado.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Fecha y hora en que se confirmó el pago (UTC). Null si no ha sido pagado.</summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>Identificador de la orden de compra asociada.</summary>
    public string? OrderId { get; set; }

    /// <summary>Email o identificador del usuario que realizó la reserva.</summary>
    public string? ReservedBy { get; set; }

    /// <summary>Versión del registro para control de concurrencia optimista. Se incrementa en cada actualización.</summary>
    public int Version { get; set; }

    /// <summary>Evento al que pertenece este ticket.</summary>
    public Event Event { get; set; } = null!;

    /// <summary>Colección de pagos asociados a este ticket.</summary>
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    /// <summary>Historial de cambios de estado del ticket.</summary>
    public ICollection<TicketHistory> History { get; set; } = new List<TicketHistory>();
}