using AuthService.Domain.Entities;

namespace AuthService.Domain.Ports.Output;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(string email);
    Task<User?> FindByEmailAsync(string email);
    Task SaveAsync(User user);
    Task UpdateAsync(User user);
}
