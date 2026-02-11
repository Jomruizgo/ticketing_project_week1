namespace Producer.Models;

/// <summary>
/// Evento publicado cuando un pago es aprobado
/// </summary>
public class PaymentApprovedEvent
{
    /// <summary>
    /// ID del ticket
    /// </summary>
    public int TicketId { get; set; }

    /// <summary>
    /// ID del evento
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// Monto pagado en centavos
    /// </summary>
    public int AmountCents { get; set; }

    /// <summary>
    /// Moneda
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Email del pagador
    /// </summary>
    public required string PaymentBy { get; set; }

    /// <summary>
    /// Referencia de transacción
    /// </summary>
    public required string TransactionRef { get; set; }

    /// <summary>
    /// Timestamp de cuando se aprobó el pago
    /// </summary>
    public DateTime ApprovedAt { get; set; }
}

/// <summary>
/// Evento publicado cuando un pago es rechazado
/// </summary>
public class PaymentRejectedEvent
{
    /// <summary>
    /// ID del ticket
    /// </summary>
    public int TicketId { get; set; }

    /// <summary>
    /// ID del evento
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// Monto intendado en centavos
    /// </summary>
    public int AmountCents { get; set; }

    /// <summary>
    /// Moneda
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Email del pagador
    /// </summary>
    public required string PaymentBy { get; set; }

    /// <summary>
    /// Razón del rechazo
    /// </summary>
    public required string RejectionReason { get; set; }

    /// <summary>
    /// Referencia de transacción (si existe)
    /// </summary>
    public string? TransactionRef { get; set; }

    /// <summary>
    /// Timestamp de cuando se rechazó el pago
    /// </summary>
    public DateTime RejectedAt { get; set; }
}
