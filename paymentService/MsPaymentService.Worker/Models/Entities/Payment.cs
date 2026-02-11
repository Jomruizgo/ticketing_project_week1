namespace MsPaymentService.Worker.Models.Entities;

/// <summary>
/// Estados posibles de un pago en su ciclo de vida.
/// Los valores están en minúsculas para coincidir con los enums nativos de PostgreSQL.
/// </summary>
public enum PaymentStatus
{
    /// <summary>Pago en proceso, pendiente de confirmación.</summary>
    pending,
    /// <summary>Pago aprobado exitosamente por el proveedor.</summary>
    approved,
    /// <summary>Pago fallido o rechazado por el proveedor.</summary>
    failed,
    /// <summary>Pago expirado por timeout de la reserva.</summary>
    expired
}

/// <summary>
/// Representa un pago asociado a un ticket.
/// Registra el estado del pago, referencia del proveedor, monto y moneda.
/// </summary>
public class Payment
{
    /// <summary>Identificador único del pago.</summary>
    public long Id { get; set; }

    /// <summary>Identificador del ticket asociado a este pago.</summary>
    public long TicketId { get; set; }

    /// <summary>Estado actual del pago.</summary>
    public PaymentStatus Status { get; set; }

    /// <summary>Referencia del proveedor de pagos (ej: ID de transacción externo).</summary>
    public string? ProviderRef { get; set; }

    /// <summary>Monto del pago en centavos para evitar decimales.</summary>
    public int AmountCents { get; set; }

    /// <summary>Código de moneda ISO 4217 (ej: USD, PEN).</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Fecha y hora de creación del registro de pago (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Fecha y hora de la última actualización del pago (UTC).</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Ticket asociado a este pago.</summary>
    public Ticket Ticket { get; set; } = null!;
}