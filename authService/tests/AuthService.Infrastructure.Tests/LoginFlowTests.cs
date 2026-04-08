using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuthService.Application.DTOs;

namespace AuthService.Infrastructure.Tests;

[Collection("Integration")]
public sealed class LoginFlowTests
{
    private readonly HttpClient _client;

    public LoginFlowTests(AuthServiceFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task RegisterUserAsync(string email, string firstName = "Test", string lastName = "User")
    {
        var request = new
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Password = "Segura@123",
            ConfirmPassword = "Segura@123"
        };
        await _client.PostAsJsonAsync("/api/auth/register", request);
    }

    [Fact]
    public async Task Login_Success_Returns200WithToken()
    {
        // Arrange
        var email = "login.success@example.com";
        await RegisterUserAsync(email, "Login Success User");

        var loginRequest = new { Email = email, Password = "Segura@123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body.Token));
        Assert.True(body.ExpiresIn > 0);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        // Arrange
        var email = "login.wrongpw@example.com";
        await RegisterUserAsync(email);

        var loginRequest = new { Email = email, Password = "Incorrecta@999" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ThreeFailedAttempts_AccountLocked_Returns401()
    {
        // Arrange
        var email = "login.lockout@example.com";
        await RegisterUserAsync(email, "Lockout Test User");

        var badLogin = new { Email = email, Password = "Incorrecta@999" };

        // Act — 3 intentos fallidos consecutivos
        await _client.PostAsJsonAsync("/api/auth/login", badLogin);
        await _client.PostAsJsonAsync("/api/auth/login", badLogin);
        var thirdResponse = await _client.PostAsJsonAsync("/api/auth/login", badLogin);

        // Assert — el tercer intento bloquea la cuenta → 401
        Assert.Equal(HttpStatusCode.Unauthorized, thirdResponse.StatusCode);

        // Un cuarto intento con la contraseña CORRECTA también retorna 401 (cuenta bloqueada)
        var correctLogin = new { Email = email, Password = "Segura@123" };
        var lockedResponse = await _client.PostAsJsonAsync("/api/auth/login", correctLogin);
        Assert.Equal(HttpStatusCode.Unauthorized, lockedResponse.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        // Arrange — email que no existe
        var loginRequest = new { Email = "noexiste@example.com", Password = "Segura@123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidToken_Returns200()
    {
        // Arrange — registrar y hacer login para obtener token
        var email = "logout.test@example.com";
        await RegisterUserAsync(email, "Logout Test User");

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login", new { Email = email, Password = "Segura@123" });

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginBody);

        // Configurar el header Authorization con el token
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginBody.Token);

        // Act
        var logoutResponse = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // Limpiar header para no afectar otros tests
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Logout_WithoutToken_Returns401()
    {
        // Act — llamar logout sin Authorization header
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
