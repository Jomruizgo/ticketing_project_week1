namespace Producer.Application.UseCases.ReserveTicket;

public record ReserveTicketCommand(
    long EventId,
    long TicketId,
    string? OrderId,
    string? ReservedBy,
    int ExpiresInSeconds);
