namespace AuthService.Infrastructure.Services;

public sealed class JwtSettings
{
    public string Secret { get; init; } = default!;
    public int ExpirationMinutes { get; init; } = 60;
    public string Issuer { get; init; } = default!;
    public string Audience { get; init; } = default!;
}
