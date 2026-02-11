namespace MsPaymentService.Worker.Models.Events;

/// <summary>
/// Evento genérico de pago de ticket recibido desde el broker de mensajería.
/// Representa la información base de una reserva de ticket que entra al flujo de pago.
/// </summary>
public class TicketPaymentEvent
{
    /// <summary>Identificador del ticket.</summary>
    public long TicketId { get; set; }

    /// <summary>Identificador del evento (concierto, espectáculo) asociado.</summary>
    public long EventId { get; set; }

    /// <summary>Identificador de la orden de compra.</summary>
    public string OrderId { get; set; } = default!;

    /// <summary>Email o identificador del usuario que realizó la reserva.</summary>
    public string ReservedBy { get; set; } = default!;

    /// <summary>Duración de la reserva en segundos (TTL).</summary>
    public int ReservationDurationSeconds { get; set; }

    /// <summary>Fecha y hora de publicación del evento en el broker (UTC).</summary>
    public DateTime PublishedAt { get; set; }
}
