using CrudService.Application.Dtos;
using CrudService.Application.UseCases.Waitlist.EnrollInWaitlist;
using CrudService.Application.UseCases.Waitlist.GetWaitlistStatus;
using CrudService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CrudService.Api.Controllers;

[ApiController]
[Route("api/waitlist")]
public class WaitlistController : ControllerBase
{
    private readonly IEnrollInWaitlistUseCase _enrollInWaitlistUseCase;
    private readonly IGetWaitlistStatusUseCase _getWaitlistStatusUseCase;

    public WaitlistController(
        IEnrollInWaitlistUseCase enrollInWaitlistUseCase,
        IGetWaitlistStatusUseCase getWaitlistStatusUseCase)
    {
        _enrollInWaitlistUseCase = enrollInWaitlistUseCase;
        _getWaitlistStatusUseCase = getWaitlistStatusUseCase;
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
