namespace MsPaymentService.Worker.Models.Events;

/// <summary>
/// Evento recibido desde RabbitMQ que indica que un pago fue rechazado.
/// Contiene la información del rechazo para realizar la liberación del ticket.
/// </summary>
public class PaymentRejectedEvent
{
    /// <summary>Identificador del ticket cuyo pago fue rechazado.</summary>
    public long TicketId { get; set; }

    /// <summary>Identificador del pago rechazado.</summary>
    public long PaymentId { get; set; }

    /// <summary>Referencia del proveedor de pagos.</summary>
    public string? ProviderReference { get; set; }

    /// <summary>Motivo del rechazo del pago.</summary>
    public string RejectionReason { get; set; } = string.Empty;

    /// <summary>Fecha y hora del rechazo (UTC).</summary>
    public DateTime RejectedAt { get; set; }

    /// <summary>Identificador único del evento de mensajería.</summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>Timestamp del evento de mensajería (UTC).</summary>
    public DateTime EventTimestamp { get; set; }
}