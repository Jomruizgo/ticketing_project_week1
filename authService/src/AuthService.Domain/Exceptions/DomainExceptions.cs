namespace AuthService.Domain.Exceptions;

public sealed class EmailAlreadyExistsException : Exception
{
    public EmailAlreadyExistsException(string email)
        : base($"El correo '{email}' ya está registrado.") { }
}

public sealed class InvalidPasswordException : Exception
{
    public InvalidPasswordException()
        : base("La contraseña no cumple los requisitos mínimos: mínimo 8 caracteres, al menos 1 mayúscula y 1 carácter especial.") { }
}

public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("Credenciales inválidas.") { }
}

public sealed class AccountLockedException : Exception
{
    public AccountLockedException()
        : base("Credenciales inválidas.") { }
}
