namespace CrudService.Infrastructure.Sse;

public interface IWaitlistSseNotifier
{
    Task SendEventAsync(string email, string eventType, string jsonPayload);
}
