using ReservationService.Application.UseCases.ProcessReservation;

namespace ReservationService.Application.Interfaces;

public interface IProcessReservationUseCase
{
    Task<ProcessReservationResponse> HandleAsync(
        ProcessReservationCommand command,
        CancellationToken cancellationToken = default);
}