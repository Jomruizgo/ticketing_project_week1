using MsPaymentService.Worker.Models.Entities;

namespace MsPaymentService.Worker.Models.DTOs;

/// <summary>
/// DTO de respuesta que representa la información pública de un pago.
/// Utilizado para exponer datos del pago sin incluir relaciones de navegación.
/// </summary>
public class PaymentResponse
{
    /// <summary>Identificador único del pago.</summary>
    public long Id { get; set; }

    /// <summary>Identificador del ticket asociado.</summary>
    public long TicketId { get; set; }

    /// <summary>Estado actual del pago.</summary>
    public PaymentStatus Status { get; set; }

    /// <summary>Referencia del proveedor de pagos.</summary>
    public string? ProviderRef { get; set; }

    /// <summary>Fecha de creación del pago (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Fecha de última actualización del pago (UTC).</summary>
    public DateTime UpdatedAt { get; set; }
}