namespace CrudService.Application.UseCases.Waitlist.EnrollInWaitlist;

using CrudService.Application.Dtos;

public interface IEnrollInWaitlistUseCase
{
    Task<WaitlistEntryDto> HandleAsync(EnrollInWaitlistCommand command);
}
