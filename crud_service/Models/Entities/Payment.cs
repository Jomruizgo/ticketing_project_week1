namespace CrudService.Models.Entities;

/// <summary>
/// Estados posibles de un pago
/// </summary>
public enum PaymentStatus
{
    Pending,
    Approved,
    Failed,
    Expired
}

/// <summary>
/// Representa un pago en el sistema
/// </summary>
public class Payment
{
    /// <summary>
    /// ID único del pago
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// ID del ticket asociado
    /// </summary>
    public long TicketId { get; set; }

    /// <summary>
    /// Estado del pago
    /// </summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// Referencia del proveedor de pagos
    /// </summary>
    public string? ProviderRef { get; set; }

    /// <summary>
    /// Monto en centavos
    /// </summary>
    public int AmountCents { get; set; }

    /// <summary>
    /// Moneda del pago
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Fecha de creación del pago
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha de actualización del pago
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ticket asociado
    /// </summary>
    public Ticket Ticket { get; set; } = null!;
}
