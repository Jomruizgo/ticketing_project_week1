using ReservationService.Application.DTOs.ProcessReservation;

namespace ReservationService.Application.Interfaces;

public interface IProcessReservationUseCase
{
    Task<ProcessReservationResponse> HandleAsync(
        ProcessReservationCommand command,
        CancellationToken cancellationToken = default);
}