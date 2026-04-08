namespace CrudService.Application.UseCases.Waitlist.GetWaitlistStatus;

using CrudService.Application.Dtos;

public interface IGetWaitlistStatusUseCase
{
    Task<WaitlistStatusResponse?> HandleAsync(GetWaitlistStatusQuery query);
}
