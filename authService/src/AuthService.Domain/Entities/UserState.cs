using AuthService.Domain.Exceptions;
using AuthService.Domain.Ports.Output;

namespace AuthService.Domain.Entities;

public abstract class UserState
{
    protected User User { get; }

    protected UserState(User user) => User = user;

    public abstract void AttemptLogin(string plainPassword, IPasswordHashingService passwordService);
}

public sealed class ActiveState : UserState
{
    public ActiveState(User user) : base(user) { }

    public override void AttemptLogin(string plainPassword, IPasswordHashingService passwordService)
    {
        bool valid = passwordService.Verify(plainPassword, User.PasswordHash);
        if (!valid)
        {
            User.RecordFailedAttempt();
            if (User.IsLocked())
                throw new AccountLockedException();
            throw new InvalidCredentialsException();
        }

        User.ResetFailedAttempts();
    }
}

public sealed class LockedState : UserState
{
    public LockedState(User user) : base(user) { }

    public override void AttemptLogin(string plainPassword, IPasswordHashingService passwordService)
    {
        // Never verify password when locked — prevents timing attacks and reveals no info
        throw new AccountLockedException();
    }
}
