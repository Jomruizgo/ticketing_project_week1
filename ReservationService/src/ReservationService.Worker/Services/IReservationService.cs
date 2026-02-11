using ReservationService.Worker.Models;

namespace ReservationService.Worker.Services;

public interface IReservationService
{
    Task<ReservationResult> ProcessReservationAsync(ReservationMessage message, CancellationToken cancellationToken = default);
}

public record ReservationResult(bool Success, string? ErrorMessage = null);
