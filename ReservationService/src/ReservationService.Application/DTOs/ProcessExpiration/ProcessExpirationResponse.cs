namespace ReservationService.Application.DTOs.ProcessExpiration;

public record ProcessExpirationResponse(bool Success, bool StatusChanged = false, string? ErrorMessage = null);
