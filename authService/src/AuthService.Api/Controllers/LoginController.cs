using AuthService.Application.DTOs;
using AuthService.Application.Ports.Input;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class LoginController : ControllerBase
{
    private readonly ILoginUserUseCase _loginUserUseCase;

    public LoginController(ILoginUserUseCase loginUserUseCase)
        => _loginUserUseCase = loginUserUseCase;

    /// <summary>HU2: Inicio de sesión.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new
            {
                message = "Datos inválidos",
                errors = ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(
                        e => e.Key,
                        e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray())
            });

        string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var response = await _loginUserUseCase.ExecuteAsync(request, ip);
        return Ok(response);
    }

    /// <summary>HU2: Cierre de sesión (stateless — el cliente elimina el token).</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
        => Ok(new { message = "Logout exitoso" });
}
