using System.Threading.Channels;

namespace CrudService.Application.Interfaces;

public interface ITicketStatusNotifier
{
    void Notify(long ticketId, string newStatus);
}

public interface ITicketStatusSubscriber
{
    ChannelReader<TicketStatusUpdate> Subscribe(long ticketId);
}
