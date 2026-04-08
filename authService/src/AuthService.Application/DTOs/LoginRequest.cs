using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    public string Email { get; init; } = default!;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; init; } = default!;
}

public sealed record LoginResponse(string Token, int ExpiresIn);
