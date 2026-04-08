using System.Text.Json;
using AuthService.Domain.Exceptions;

namespace AuthService.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (EmailAlreadyExistsException ex)
        {
            await WriteJsonAsync(context, StatusCodes.Status409Conflict,
                new { message = ex.Message });
        }
        catch (InvalidPasswordException ex)
        {
            await WriteJsonAsync(context, StatusCodes.Status400BadRequest,
                new { message = "Datos inválidos", errors = new { password = ex.Message } });
        }
        catch (InvalidCredentialsException)
        {
            await WriteJsonAsync(context, StatusCodes.Status401Unauthorized,
                new { message = "Credenciales inválidas" });
        }
        catch (AccountLockedException)
        {
            // Same generic message — do NOT reveal account is locked (anti-enumeration)
            await WriteJsonAsync(context, StatusCodes.Status401Unauthorized,
                new { message = "Credenciales inválidas" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteJsonAsync(context, StatusCodes.Status500InternalServerError,
                new { message = "Ha ocurrido un error interno. Intente de nuevo más tarde." });
        }
    }

    private static async Task WriteJsonAsync(HttpContext context, int statusCode, object body)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(body,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
