namespace CrudService.Application.Interfaces;

public interface IWaitlistSseNotifier
{
    Task SendEventAsync(string email, string eventType, string jsonPayload);
}
