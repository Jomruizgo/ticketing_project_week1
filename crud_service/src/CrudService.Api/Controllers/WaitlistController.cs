using System.Globalization;
using CrudService.Application.Dtos;
using CrudService.Application.UseCases.Waitlist.EnrollInWaitlist;
using CrudService.Application.UseCases.Waitlist.GetWaitlistStatus;
using CrudService.Domain.Exceptions;
using CrudService.Infrastructure.Sse;
using Microsoft.AspNetCore.Mvc;

namespace CrudService.Api.Controllers;

[ApiController]
[Route("api/waitlist")]
public class WaitlistController : ControllerBase
{
    private readonly IEnrollInWaitlistUseCase _enrollInWaitlistUseCase;
    private readonly IGetWaitlistStatusUseCase _getWaitlistStatusUseCase;
    private readonly IWaitlistSseSubscriber _sseSubscriber;

    private static readonly int MaxConnectionsPerEmail =
        int.TryParse(Environment.GetEnvironmentVariable("SSE_MAX_CONNECTIONS_PER_EMAIL"), out var max) ? max : 5;

    private static double GetKeepaliveIntervalSeconds() =>
        double.TryParse(
            Environment.GetEnvironmentVariable("SSE_KEEPALIVE_INTERVAL_SECONDS"),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var sec) ? sec : 30;

    public WaitlistController(
        IEnrollInWaitlistUseCase enrollInWaitlistUseCase,
        IGetWaitlistStatusUseCase getWaitlistStatusUseCase,
        IWaitlistSseSubscriber sseSubscriber)
    {
        _enrollInWaitlistUseCase = enrollInWaitlistUseCase;
        _getWaitlistStatusUseCase = getWaitlistStatusUseCase;
        _sseSubscriber = sseSubscriber;
    }

    [HttpGet("entries")]
    public async Task<IActionResult> GetWaitlistStatus(
        [FromQuery] long? eventId,
        [FromQuery] string? email)
    {
        if (!eventId.HasValue || string.IsNullOrWhiteSpace(email))
            return BadRequest(new { detail = "Both 'eventId' and 'email' query parameters are required." });

        if (!IsValidEmail(email))
            return BadRequest(new { detail = "Invalid email format." });

        var query = new GetWaitlistStatusQuery(eventId.Value, email);
        var result = await _getWaitlistStatusUseCase.HandleAsync(query);

        if (result is null)
            return NotFound(new { detail = "No waitlist entry found for the given event and email." });

        return Ok(result);
    }

    [HttpPost("entries")]
    public async Task<ActionResult<WaitlistEntryDto>> EnrollInWaitlist(
        [FromBody] EnrollInWaitlistRequest request)
    {
        try
        {
            var command = new EnrollInWaitlistCommand(request.EventId, request.BuyerEmail);
            var entry = await _enrollInWaitlistUseCase.HandleAsync(command);
            return StatusCode(StatusCodes.Status201Created, entry);
        }
        catch (EventNotFoundException)
        {
            return NotFound(new { detail = $"Event with id {request.EventId} was not found." });
        }
        catch (WaitlistClosedException)
        {
            return UnprocessableEntity(new { detail = "The waitlist for this event is closed." });
        }
        catch (DuplicateWaitlistEntryException)
        {
            return Conflict(new { detail = "An active waitlist entry already exists for this buyer and event." });
        }
    }

    [HttpGet("stream")]
    public async Task<IActionResult> StreamSse([FromQuery] string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            return BadRequest(new { detail = "A valid 'email' query parameter is required." });

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (_sseSubscriber.GetConnectionCount(normalizedEmail) >= MaxConnectionsPerEmail)
            return StatusCode(429, new { detail = "Too many active SSE connections for this email." });

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var ct = HttpContext.RequestAborted;
        var client = await _sseSubscriber.RegisterAsync(normalizedEmail, Response, ct);

        try
        {
            var keepaliveInterval = TimeSpan.FromSeconds(GetKeepaliveIntervalSeconds());
            var keepaliveBytes = System.Text.Encoding.UTF8.GetBytes(": keepalive\n\n");

            while (!ct.IsCancellationRequested)
            {
                using var delayCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                delayCts.CancelAfter(keepaliveInterval);

                try
                {
                    if (await client.EventChannel.Reader.WaitToReadAsync(delayCts.Token))
                    {
                        while (client.EventChannel.Reader.TryRead(out var sseEvent))
                        {
                            var formatted = $"event: {sseEvent.EventType}\ndata: {sseEvent.Data}\n\n";
                            await Response.Body.WriteAsync(
                                System.Text.Encoding.UTF8.GetBytes(formatted), ct);
                            await Response.Body.FlushAsync(ct);
                        }
                    }
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    await Response.Body.WriteAsync(keepaliveBytes, ct);
                    await Response.Body.FlushAsync(ct);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected
        }
        finally
        {
            await _sseSubscriber.UnregisterAsync(client);
        }

        return new EmptyResult();
    }

    private static bool IsValidEmail(string email)
    {
        var trimmed = email.Trim();
        if (trimmed.Length == 0) return false;
        var atIndex = trimmed.IndexOf('@');
        return atIndex > 0
            && atIndex < trimmed.Length - 1
            && trimmed.IndexOf('@', atIndex + 1) < 0
            && trimmed[^1] != '.';
    }
}
