namespace AuthService.Infrastructure.Persistence;

/// <summary>EF Core entity for login audit log.</summary>
public sealed class LoginAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string EmailAttempted { get; set; } = default!;
    public string? IpAddress { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public bool Successful { get; set; }
}
