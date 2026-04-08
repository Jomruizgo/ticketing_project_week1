using CrudService.Application.Dtos;
using CrudService.Application.UseCases.Waitlist.EnrollInWaitlist;
using CrudService.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CrudService.Api.Controllers;

[ApiController]
[Route("api/waitlist")]
public class WaitlistController : ControllerBase
{
    private readonly IEnrollInWaitlistUseCase _enrollInWaitlistUseCase;

    public WaitlistController(IEnrollInWaitlistUseCase enrollInWaitlistUseCase)
    {
        _enrollInWaitlistUseCase = enrollInWaitlistUseCase;
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
}
