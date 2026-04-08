using System.Collections.Concurrent;
using System.Threading.Channels;
using CrudService.Application.Interfaces;

namespace CrudService.Infrastructure.Messaging;

/// <summary>
/// Singleton que correlaciona ticketId con las conexiones SSE que esperan su cambio de estado.
/// Cuando llega un evento de RabbitMQ, notifica a todos los listeners de ese ticket.
/// ISP: implementa interfaces segregadas — ITicketStatusNotifier (para el producer) e ITicketStatusSubscriber (para el controller SSE).
/// </summary>
public class TicketStatusHub : ITicketStatusNotifier, ITicketStatusSubscriber
{
    private readonly ConcurrentDictionary<long, List<Channel<TicketStatusUpdate>>> _subscriptions = new();

    public ChannelReader<TicketStatusUpdate> Subscribe(long ticketId)
    {
        var channel = Channel.CreateBounded<TicketStatusUpdate>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        _subscriptions.AddOrUpdate(
            ticketId,
            _ => [channel],
            (_, existing) => { lock (existing) { existing.Add(channel); } return existing; });

        return channel.Reader;
    }

    public void Notify(long ticketId, string newStatus)
    {
        if (string.IsNullOrWhiteSpace(newStatus))
            return;

        if (!_subscriptions.TryGetValue(ticketId, out var channels))
            return;

        var update = new TicketStatusUpdate(ticketId, newStatus);

        List<Channel<TicketStatusUpdate>> snapshot;
        lock (channels)
        {
            snapshot = [.. channels];
        }

        foreach (var ch in snapshot)
            ch.Writer.TryWrite(update);

        _subscriptions.TryRemove(ticketId, out _);
    }
}
