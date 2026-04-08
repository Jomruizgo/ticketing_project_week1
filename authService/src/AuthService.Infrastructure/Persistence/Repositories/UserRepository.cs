using AuthService.Domain.Entities;
using AuthService.Domain.Ports.Output;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public async Task<bool> ExistsByEmailAsync(string email)
        => await _context.Users.AnyAsync(u => u.Email == email.ToLowerInvariant());

    public async Task<User?> FindByEmailAsync(string email)
        => await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

    public async Task SaveAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}
