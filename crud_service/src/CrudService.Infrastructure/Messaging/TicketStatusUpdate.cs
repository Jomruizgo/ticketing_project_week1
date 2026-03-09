namespace CrudService.Infrastructure.Messaging;

public record TicketStatusUpdate(long TicketId, string NewStatus);
