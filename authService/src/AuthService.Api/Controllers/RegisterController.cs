using AuthService.Application.DTOs;
using AuthService.Application.Ports.Input;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class RegisterController : ControllerBase
{
    private readonly IRegisterUserUseCase _registerUserUseCase;

    public RegisterController(IRegisterUserUseCase registerUserUseCase)
        => _registerUserUseCase = registerUserUseCase;

    /// <summary>HU1: Registro de nuevo usuario comprador.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
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

        var response = await _registerUserUseCase.ExecuteAsync(request);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
