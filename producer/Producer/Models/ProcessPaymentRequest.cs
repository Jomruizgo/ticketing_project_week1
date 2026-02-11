namespace Producer.Models;

/// <summary>
/// Solicitud para procesar un pago de ticket
/// </summary>
public class ProcessPaymentRequest
{
    /// <summary>
    /// ID del ticket para el cual se procesa el pago
    /// </summary>
    public int TicketId { get; set; }

    /// <summary>
    /// ID del evento asociado
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// Cantidad en centavos (para evitar decimales)
    /// </summary>
    public int AmountCents { get; set; }

    /// <summary>
    /// Moneda (default: USD)
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Email del cliente que realiza el pago
    /// </summary>
    public required string PaymentBy { get; set; }

    /// <summary>
    /// ID del método de pago (ej: tarjeta, paypal, etc)
    /// </summary>
    public required string PaymentMethodId { get; set; }

    /// <summary>
    /// Referencia o ID de transacción externa
    /// </summary>
    public string? TransactionRef { get; set; }
}
