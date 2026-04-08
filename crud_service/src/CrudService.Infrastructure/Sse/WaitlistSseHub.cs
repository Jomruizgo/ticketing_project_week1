using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

namespace CrudService.Infrastructure.Sse;

public class WaitlistSseHub : IWaitlistSseNotifier, IWaitlistSseSubscriber
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<SseClient>> _clients = new();

    public Task<SseClient> RegisterAsync(string email, HttpResponse response, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var client = new SseClient(normalizedEmail, response, cancellationToken);

        _clients.AddOrUpdate(
            normalizedEmail,
            _ => new ConcurrentBag<SseClient> { client },
            (_, bag) => { bag.Add(client); return bag; });

        return Task.FromResult(client);
    }

    public Task UnregisterAsync(SseClient client)
    {
        if (_clients.TryGetValue(client.Email, out var bag))
        {
            var remaining = new ConcurrentBag<SseClient>(bag.Where(c => c.Id != client.Id));
            if (remaining.IsEmpty)
                _clients.TryRemove(client.Email, out _);
            else
                _clients[client.Email] = remaining;
        }

        return Task.CompletedTask;
    }

    public int GetConnectionCount(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _clients.TryGetValue(normalizedEmail, out var bag) ? bag.Count : 0;
    }

    public Task SendEventAsync(string email, string eventType, string jsonPayload)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!_clients.TryGetValue(normalizedEmail, out var bag))
            return Task.CompletedTask;

        var sseEvent = new SseEvent(eventType, jsonPayload);

        foreach (var client in bag)
            client.EventChannel.Writer.TryWrite(sseEvent);

        return Task.CompletedTask;
    }
}
