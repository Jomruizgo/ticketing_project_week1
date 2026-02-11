using Producer.Models;

namespace Producer.Services;

/// <summary>
/// Interfaz para publicar eventos de pago a RabbitMQ
/// </summary>
public interface IPaymentPublisher
{
    /// <summary>
    /// Publica un evento de pago aprobado
    /// </summary>
    /// <param name="paymentEvent">Evento de pago aprobado</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Task completado cuando se publica el evento</returns>
    Task PublishPaymentApprovedAsync(PaymentApprovedEvent paymentEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publica un evento de pago rechazado
    /// </summary>
    /// <param name="paymentEvent">Evento de pago rechazado</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Task completado cuando se publica el evento</returns>
    Task PublishPaymentRejectedAsync(PaymentRejectedEvent paymentEvent, CancellationToken cancellationToken = default);
}
