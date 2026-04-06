using ReservationService.Application.UseCases.ProcessExpiration;

namespace ReservationService.Application.Interfaces;

public interface IProcessExpirationUseCase
{
    Task<ProcessExpirationResponse> HandleAsync(
        ProcessExpirationCommand command,
        CancellationToken cancellationToken = default);
}
