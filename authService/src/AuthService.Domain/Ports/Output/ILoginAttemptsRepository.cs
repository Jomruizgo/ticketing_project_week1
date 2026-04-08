namespace AuthService.Domain.Ports.Output;

public interface ILoginAttemptsRepository
{
    Task RecordAttemptAsync(Guid? userId, string emailAttempted, string? ipAddress, bool successful);
    Task<int> CountRecentFailuresAsync(string email, TimeSpan window);
}
