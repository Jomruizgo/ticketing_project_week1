namespace Producer.Application.DTOs.ReserveTicket;

public record ReserveTicketCommand(
    long EventId,
    long TicketId,
    string? OrderId,
    string? ReservedBy,
    int ExpiresInSeconds);
