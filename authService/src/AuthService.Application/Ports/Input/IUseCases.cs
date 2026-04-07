using AuthService.Application.DTOs;

namespace AuthService.Application.Ports.Input;

public interface IRegisterUserUseCase
{
    Task<RegisterUserResponse> ExecuteAsync(RegisterUserRequest request);
}

public interface ILoginUserUseCase
{
    Task<LoginResponse> ExecuteAsync(LoginRequest request, string? ipAddress = null);
}
