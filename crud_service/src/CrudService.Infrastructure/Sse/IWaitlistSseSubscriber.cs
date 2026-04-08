using Microsoft.AspNetCore.Http;

namespace CrudService.Infrastructure.Sse;

public interface IWaitlistSseSubscriber
{
    Task<SseClient> RegisterAsync(string email, HttpResponse response, CancellationToken cancellationToken);
    Task UnregisterAsync(SseClient client);
    int GetConnectionCount(string email);
}
