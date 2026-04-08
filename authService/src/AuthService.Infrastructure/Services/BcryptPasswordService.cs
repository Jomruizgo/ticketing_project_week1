using AuthService.Domain.Ports.Output;
using BC = BCrypt.Net.BCrypt;

namespace AuthService.Infrastructure.Services;

public sealed class BcryptPasswordService : IPasswordHashingService
{
    private const int WorkFactor = 12;

    public string Hash(string plainPassword)
        => BC.HashPassword(plainPassword, WorkFactor);

    public bool Verify(string plainPassword, string hashedPassword)
        => BC.Verify(plainPassword, hashedPassword);
}
