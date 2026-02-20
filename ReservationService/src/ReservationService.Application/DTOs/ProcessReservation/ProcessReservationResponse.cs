namespace ReservationService.Application.DTOs.ProcessReservation;

public record ProcessReservationResponse(bool Success, string? ErrorMessage = null);
