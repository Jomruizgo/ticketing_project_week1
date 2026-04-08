using System.Threading.Channels;
using Microsoft.AspNetCore.Http;

namespace CrudService.Infrastructure.Sse;

public class SseClient
{
    public string Id { get; }
    public string Email { get; }
    public HttpResponse Response { get; }
    public Channel<SseEvent> EventChannel { get; }
    public CancellationToken CancellationToken { get; }
    public DateTime ConnectedAt { get; }

    public SseClient(string email, HttpResponse response, CancellationToken cancellationToken)
    {
        Id = Guid.NewGuid().ToString();
        Email = email;
        Response = response;
        EventChannel = Channel.CreateUnbounded<SseEvent>();
        CancellationToken = cancellationToken;
        ConnectedAt = DateTime.UtcNow;
    }
}
