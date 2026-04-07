namespace AuthService.Api.Settings;

public sealed class JwtSettings
{
    public string Secret { get; init; } = default!;
    public int ExpirationMinutes { get; init; } = 60;
    public string Issuer { get; init; } = default!;
    public string Audience { get; init; } = default!;
}

public sealed class LockoutSettings
{
    public int MaxFailedAttempts { get; init; } = 3;
    public int LockoutMinutes { get; init; } = 15;
}
