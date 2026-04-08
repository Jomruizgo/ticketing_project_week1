using AuthService.Application.DTOs;
using AuthService.Application.UseCases;
using AuthService.Domain.Entities;
using AuthService.Domain.Exceptions;
using AuthService.Domain.Ports.Output;
using NSubstitute;

namespace AuthService.Application.Tests;

public sealed class LoginUserUseCaseTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHashingService _passwordService = Substitute.For<IPasswordHashingService>();
    private readonly ILoginAttemptsRepository _attemptsRepo = Substitute.For<ILoginAttemptsRepository>();
    private readonly IJwtTokenService _jwtService = Substitute.For<IJwtTokenService>();
    private readonly LoginUserUseCase _sut;

    public LoginUserUseCaseTests()
    {
        _sut = new LoginUserUseCase(_userRepository, _passwordService, _attemptsRepo, _jwtService);
    }

    private static User CreateTestUser(string password = "Secr3t@Pass")
    {
        // create a valid user with a known hash
        var hash = BCrypt.Net.BCrypt.HashPassword(password, 4); // low cost for tests
        return User.Create("Ana", "Perez", "ana.perez@example.com", hash);
    }

    [Fact]
    public async Task Success_ReturnsToken_WhenCredentialsValid()
    {
        // Arrange
        var user = CreateTestUser();
        _userRepository.FindByEmailAsync("ana.perez@example.com").Returns(user);
        _passwordService.Verify("Secr3t@Pass", user.PasswordHash).Returns(true);
        _jwtService.GenerateToken(user).Returns(("jwt_token", 3600));

        var request = new LoginRequest { Email = "ana.perez@example.com", Password = "Secr3t@Pass" };

        // Act
        var result = await _sut.ExecuteAsync(request);

        // Assert
        Assert.Equal("jwt_token", result.Token);
        Assert.Equal(3600, result.ExpiresIn);
        await _attemptsRepo.Received(1).RecordAttemptAsync(user.Id, "ana.perez@example.com", null, true);
    }

    [Fact]
    public async Task Fails_WhenUserNotFound_ThrowsInvalidCredentialsException()
    {
        // Arrange
        _userRepository.FindByEmailAsync(Arg.Any<string>()).Returns((User?)null);
        _passwordService.Hash(Arg.Any<string>()).Returns("dummy_hash"); // timing equalization

        var request = new LoginRequest { Email = "notexist@example.com", Password = "Secr3t@Pass" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _sut.ExecuteAsync(request));
        // Must still record failed attempt (prevents timing-based user enumeration)
        await _attemptsRepo.Received(1).RecordAttemptAsync(null, "notexist@example.com", null, false);
        // Must NOT generate token
        _jwtService.DidNotReceive().GenerateToken(Arg.Any<User>());
    }

    [Fact]
    public async Task Fails_WhenPasswordInvalid_RecordsAttempt()
    {
        // Arrange
        var user = CreateTestUser();
        _userRepository.FindByEmailAsync("ana.perez@example.com").Returns(user);
        _passwordService.Verify("wrongpassword", user.PasswordHash).Returns(false);

        var request = new LoginRequest { Email = "ana.perez@example.com", Password = "wrongpassword" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidCredentialsException>(() => _sut.ExecuteAsync(request));
        await _attemptsRepo.Received(1).RecordAttemptAsync(user.Id, "ana.perez@example.com", null, false);
    }

    [Fact]
    public async Task LocksAccount_AfterThreeFailures()
    {
        // Arrange
        var user = User.Create("Ana", "Perez", "ana.perez@example.com", "hash");
        _userRepository.FindByEmailAsync("ana.perez@example.com").Returns(user);
        _passwordService.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var request = new LoginRequest { Email = "ana.perez@example.com", Password = "wrong" };

        // Act — 3 failed attempts should trigger lock
        for (int i = 0; i < 3; i++)
        {
            try { await _sut.ExecuteAsync(request); } catch { /* expected */ }
        }

        // Assert — account must be locked
        Assert.True(user.IsLocked());
    }

    [Fact]
    public async Task ReturnsGenericError_WhenLocked()
    {
        // Arrange
        var user = User.Create("Ana", "Perez", "ana.perez@example.com", "hash");
        user.LockUntil(DateTime.UtcNow.AddMinutes(15)); // manually lock
        _userRepository.FindByEmailAsync("ana.perez@example.com").Returns(user);

        var request = new LoginRequest { Email = "ana.perez@example.com", Password = "Secr3t@Pass" };

        // Act & Assert — locked account returns AccountLockedException (mapped to generic 401)
        await Assert.ThrowsAsync<AccountLockedException>(() => _sut.ExecuteAsync(request));
        // Must NOT verify password when locked
        _passwordService.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
    }
}
