using System.Net;
using System.Net.Http.Json;
using AuthService.Application.DTOs;

namespace AuthService.Infrastructure.Tests;

[Collection("Integration")]
public sealed class RegisterFlowTests
{
    private readonly HttpClient _client;

    public RegisterFlowTests(AuthServiceFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_Success_Returns201WithUserInfo()
    {
        // Arrange
        var request = new
        {
            FirstName = "María",
            LastName = "López",
            Email = "maria.lopez@example.com",
            Password = "Segura@123",
            ConfirmPassword = "Segura@123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterUserResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body.Message));
        Assert.False(string.IsNullOrEmpty(body.Redirect));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        // Arrange — registrar usuario por primera vez
        var request = new
        {
            FirstName = "Carlos",
            LastName = "Torres",
            Email = "carlos.torres.dup@example.com",
            Password = "Segura@123",
            ConfirmPassword = "Segura@123"
        };

        await _client.PostAsJsonAsync("/api/auth/register", request);

        // Act — intentar registrar el mismo email
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400()
    {
        // Arrange — contraseña sin mayúscula ni carácter especial
        var request = new
        {
            FirstName = "Test",
            LastName = "User",
            Email = "weak.pass@example.com",
            Password = "password1",
            ConfirmPassword = "password1"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_PasswordMismatch_Returns400()
    {
        // Arrange
        var request = new
        {
            FirstName = "Test",
            LastName = "User",
            Email = "mismatch@example.com",
            Password = "Segura@123",
            ConfirmPassword = "Diferente@456"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
