using AuthService.Application.DTOs;
using AuthService.Application.UseCases;
using AuthService.Domain.Entities;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Ports.Output;
using NSubstitute;

namespace AuthService.Application.Tests;

public sealed class RegisterUserUseCaseTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHashingService _passwordHashingService = Substitute.For<IPasswordHashingService>();
    private readonly RegisterUserUseCase _sut;

    public RegisterUserUseCaseTests()
    {
        _sut = new RegisterUserUseCase(_userRepository, _passwordHashingService);
    }

    [Fact]
    public async Task Success_WhenValid_CreatesUserWithHash()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            FirstName = "Ana",
            LastName = "Perez",
            Email = "ana.perez@example.com",
            Password = "Secr3t@Pass",
            ConfirmPassword = "Secr3t@Pass"
        };

        _userRepository.ExistsByEmailAsync("ana.perez@example.com").Returns(false);
        _passwordHashingService.Hash("Secr3t@Pass").Returns("hashed_password");

        // Act
        var result = await _sut.ExecuteAsync(request);

        // Assert
        Assert.Equal("Registro exitoso. Serás redirigido al login.", result.Message);
        Assert.Equal("/login", result.Redirect);
        await _userRepository.Received(1).SaveAsync(Arg.Is<User>(u =>
            u.Email == "ana.perez@example.com" &&
            u.PasswordHash == "hashed_password" &&
            u.FirstName == "Ana"));
        _passwordHashingService.Received(1).Hash("Secr3t@Pass");
    }

    [Fact]
    public async Task Fails_WhenEmailExists_ThrowsEmailAlreadyExistsException()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            FirstName = "Ana",
            LastName = "Perez",
            Email = "ana.perez@example.com",
            Password = "Secr3t@Pass",
            ConfirmPassword = "Secr3t@Pass"
        };

        _userRepository.ExistsByEmailAsync("ana.perez@example.com").Returns(true);

        // Act & Assert
        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() => _sut.ExecuteAsync(request));
        await _userRepository.DidNotReceive().SaveAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task Fails_WhenPasswordInvalid_ThrowsInvalidPasswordException()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            FirstName = "Ana",
            LastName = "Perez",
            Email = "ana.perez@example.com",
            Password = "weakpassword",  // no uppercase, no special char
            ConfirmPassword = "weakpassword"
        };

        _userRepository.ExistsByEmailAsync(Arg.Any<string>()).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidPasswordException>(() => _sut.ExecuteAsync(request));
        await _userRepository.DidNotReceive().SaveAsync(Arg.Any<User>());
        _passwordHashingService.DidNotReceive().Hash(Arg.Any<string>());
    }

    [Fact]
    public async Task NormalizesEmailToLowercase()
    {
        // Arrange
        var request = new RegisterUserRequest
        {
            FirstName = "Ana",
            LastName = "Perez",
            Email = "ANA.PEREZ@EXAMPLE.COM",
            Password = "Secr3t@Pass",
            ConfirmPassword = "Secr3t@Pass"
        };

        _userRepository.ExistsByEmailAsync("ana.perez@example.com").Returns(false);
        _passwordHashingService.Hash(Arg.Any<string>()).Returns("hash");

        // Act
        await _sut.ExecuteAsync(request);

        // Assert
        await _userRepository.Received(1).SaveAsync(Arg.Is<User>(u =>
            u.Email == "ana.perez@example.com"));
    }
}
