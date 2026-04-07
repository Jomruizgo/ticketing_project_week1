using AuthService.Domain.Ports.Output;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence.Repositories;

public sealed class LoginAttemptsRepository : ILoginAttemptsRepository
{
    private readonly AppDbContext _context;

    public LoginAttemptsRepository(AppDbContext context) => _context = context;

    public async Task RecordAttemptAsync(Guid? userId, string emailAttempted, string? ipAddress, bool successful)
    {
        var attempt = new LoginAttempt
        {
            UserId = userId,
            EmailAttempted = emailAttempted,
            IpAddress = ipAddress,
            AttemptedAt = DateTime.UtcNow,
            Successful = successful
        };
        await _context.LoginAttempts.AddAsync(attempt);
        await _context.SaveChangesAsync();
    }

    public async Task<int> CountRecentFailuresAsync(string email, TimeSpan window)
    {
        var since = DateTime.UtcNow - window;
        return await _context.LoginAttempts
            .Where(a => a.EmailAttempted == email && !a.Successful && a.AttemptedAt >= since)
            .CountAsync();
    }
}
