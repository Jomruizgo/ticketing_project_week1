using AuthService.Domain.Entities;

namespace AuthService.Domain.Ports.Output;

public interface IJwtTokenService
{
    (string Token, int ExpiresIn) GenerateToken(User user);
}
