namespace ReservationService.Application.UseCases.ProcessReservation;

public record ProcessReservationResponse(bool Success, string? ErrorMessage = null);
