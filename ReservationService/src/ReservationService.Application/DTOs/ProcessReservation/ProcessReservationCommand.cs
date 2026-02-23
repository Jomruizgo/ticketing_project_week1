namespace ReservationService.Application.DTOs.ProcessReservation;

public record ProcessReservationCommand(
    long TicketId,
    long EventId,
    string OrderId,
    string ReservedBy,
    int ReservationDurationSeconds = 300,
    DateTime? PublishedAt = null
);
