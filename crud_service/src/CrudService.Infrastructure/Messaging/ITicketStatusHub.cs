using System.Threading.Channels;

namespace CrudService.Infrastructure.Messaging;

/// <summary>
/// ISP: interfaz segregada para el lado de notificación (productor).
/// El consumer solo necesita notificar, no suscribirse.
/// </summary>
public interface ITicketStatusNotifier
{
    void Notify(long ticketId, string newStatus);
}

/// <summary>
/// ISP: interfaz segregada para el lado de suscripción (consumidor SSE).
/// El controller solo necesita suscribirse, no notificar.
/// </summary>
public interface ITicketStatusSubscriber
{
    ChannelReader<TicketStatusUpdate> Subscribe(long ticketId);
}
