namespace MsPaymentService.Worker.Models.Events;

/// <summary>
/// Evento recibido desde RabbitMQ que indica que un pago fue aprobado.
/// Contiene la información de la reserva del ticket que fue pagada.
/// </summary>
public class PaymentApprovedEvent
{
    /// <summary>Identificador del ticket reservado.</summary>
    public long TicketId { get; set; }

    /// <summary>Identificador del evento (concierto, espectáculo) asociado.</summary>
    public long EventId { get; set; }

    /// <summary>Identificador de la orden de compra.</summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>Email o identificador del usuario que realizó la reserva.</summary>
    public string ReservedBy { get; set; } = string.Empty;

    /// <summary>Duración de la reserva en segundos (TTL).</summary>
    public int ReservationDurationSeconds { get; set; }

    /// <summary>Fecha y hora de publicación del evento en el broker (UTC).</summary>
    public DateTime PublishedAt { get; set; }
}