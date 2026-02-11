using MsPaymentService.Worker.Models.DTOs;

namespace MsPaymentService.Worker.Handlers;

/// <summary>
/// Despacha un mensaje al handler que corresponde a la routing key.
/// Responsabilidad única: enrutar por routing key (OCP: nuevos tipos = nuevo handler, sin modificar dispatcher).
/// </summary>
public interface IPaymentEventDispatcher
{
    /// <summary>
    /// Busca un handler para la routing key, lo ejecuta y devuelve el resultado.
    /// Devuelve null si no hay handler registrado para esa routing key.
    /// </summary>
    Task<ValidationResult?> DispatchAsync(string queueName, string json, CancellationToken cancellationToken = default);
}
