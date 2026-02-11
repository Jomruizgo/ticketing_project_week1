using MsPaymentService.Worker.Models.DTOs;

namespace MsPaymentService.Worker.Handlers;

/// <summary>
/// Contrato para procesar un tipo de evento de pago.
/// Cada implementación atiende una routing key y deserializa + valida el mensaje.
/// </summary>
public interface IPaymentEventHandler
{
    /// <summary>Routing key que este handler procesa (ej. ticket.payments.approved).</summary>
    string QueueName { get; }

    /// <summary>Procesa el mensaje JSON y devuelve el resultado de validación.</summary>
    Task<ValidationResult> HandleAsync(string json, CancellationToken cancellationToken = default);
}
