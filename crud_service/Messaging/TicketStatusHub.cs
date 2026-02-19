using System.Collections.Concurrent;
using System.Threading.Channels;

namespace CrudService.Messaging;

/// <summary>
/// Singleton que correlaciona ticketId con las conexiones SSE que esperan su cambio de estado.
/// Cuando llega un evento de RabbitMQ, notifica a todos los listeners de ese ticket.
/// </summary>
public class TicketStatusHub
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
        if (!_subscriptions.TryGetValue(ticketId, out var channels))
            return;

        var update = new TicketStatusUpdate(ticketId, newStatus);

        List<Channel<TicketStatusUpdate>> snapshot;
        lock (channels)
        {
            snapshot = [.. channels];
        }

        foreach (var ch in snapshot)
        {
            ch.Writer.TryWrite(update);
        }

        _subscriptions.TryRemove(ticketId, out _);
    }
}

public record TicketStatusUpdate(long TicketId, string NewStatus);
