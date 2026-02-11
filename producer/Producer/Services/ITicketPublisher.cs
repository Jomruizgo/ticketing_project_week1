using Producer.Models;

namespace Producer.Services;

/// <summary>
/// Servicio para publicar eventos de ticket a RabbitMQ
/// </summary>
public interface ITicketPublisher
{
    /// <summary>
    /// Publica un evento de ticket reservado a RabbitMQ
    /// </summary>
    /// <param name="ticketEvent">Evento a publicar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Task completada cuando el evento se publique</returns>
    Task PublishTicketReservedAsync(TicketReservedEvent ticketEvent, CancellationToken cancellationToken = default);
}
