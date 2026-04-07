using AuthService.Application.DTOs;
using AuthService.Application.Ports.Input;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Ports.Output;

namespace AuthService.Application.UseCases;

public sealed class LoginUserUseCase : ILoginUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly ILoginAttemptsRepository _loginAttemptsRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginUserUseCase(
        IUserRepository userRepository,
        IPasswordHashingService passwordHashingService,
        ILoginAttemptsRepository loginAttemptsRepository,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHashingService = passwordHashingService;
        _loginAttemptsRepository = loginAttemptsRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse> ExecuteAsync(LoginRequest request, string? ipAddress = null)
    {
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.FindByEmailAsync(normalizedEmail);

        if (user is null)
        {
            // Perform dummy hash to equalize response time (prevent timing attacks / user enumeration)
            _passwordHashingService.Hash("dummy_password_to_equalize_time");
            await _loginAttemptsRepository.RecordAttemptAsync(null, normalizedEmail, ipAddress, false);
            throw new InvalidCredentialsException();
        }

        try
        {
            // Delegates to Active or Locked state — polymorphic behavior
            user.State.AttemptLogin(request.Password, _passwordHashingService);
        }
        catch
        {
            await _userRepository.UpdateAsync(user);
            await _loginAttemptsRepository.RecordAttemptAsync(user.Id, normalizedEmail, ipAddress, false);
            throw;
        }

        await _userRepository.UpdateAsync(user);
        await _loginAttemptsRepository.RecordAttemptAsync(user.Id, normalizedEmail, ipAddress, true);

        var (token, expiresIn) = _jwtTokenService.GenerateToken(user);
        return new LoginResponse(token, expiresIn);
    }
}
