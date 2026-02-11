namespace MsPaymentService.Worker.Models.DTOs;

/// <summary>
/// Resultado de la validación y procesamiento de un evento de pago.
/// Encapsula el estado de éxito, fallo o procesamiento duplicado.
/// </summary>
public class ValidationResult
{
    /// <summary>Indica si la validación y procesamiento fue exitoso.</summary>
    public bool IsSuccess { get; set; }

    /// <summary>Motivo del fallo cuando <see cref="IsSuccess"/> es false.</summary>
    public string? FailureReason { get; set; }

    /// <summary>Indica si el evento ya fue procesado previamente (idempotencia).</summary>
    public bool IsAlreadyProcessed { get; set; }

    /// <summary>
    /// Crea un resultado exitoso.
    /// </summary>
    /// <returns>Instancia de <see cref="ValidationResult"/> con <see cref="IsSuccess"/> en true.</returns>
    public static ValidationResult Success() => new() { IsSuccess = true };

    /// <summary>
    /// Crea un resultado de fallo con el motivo especificado.
    /// </summary>
    /// <param name="reason">Descripción del motivo del fallo.</param>
    /// <returns>Instancia de <see cref="ValidationResult"/> con el motivo de fallo.</returns>
    public static ValidationResult Failure(string reason) => new() 
    { 
        IsSuccess = false, 
        FailureReason = reason 
    };

    /// <summary>
    /// Crea un resultado indicando que el evento ya fue procesado previamente.
    /// Utilizado para garantizar idempotencia en el procesamiento de mensajes.
    /// </summary>
    /// <returns>Instancia de <see cref="ValidationResult"/> marcada como ya procesada.</returns>
    public static ValidationResult AlreadyProcessed() => new() 
    { 
        IsSuccess = false, 
        IsAlreadyProcessed = true,
        FailureReason = "Event already processed" 
    };
}