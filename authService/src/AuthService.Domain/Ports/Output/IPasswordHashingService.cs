namespace AuthService.Domain.Ports.Output;

public interface IPasswordHashingService
{
    string Hash(string plainPassword);
    bool Verify(string plainPassword, string hashedPassword);
}
