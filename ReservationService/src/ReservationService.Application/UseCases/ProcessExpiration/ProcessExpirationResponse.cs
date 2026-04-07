namespace ReservationService.Application.UseCases.ProcessExpiration;

public record ProcessExpirationResponse(bool Success, bool StatusChanged = false, string? ErrorMessage = null);
