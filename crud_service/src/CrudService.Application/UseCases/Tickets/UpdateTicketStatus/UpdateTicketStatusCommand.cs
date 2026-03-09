namespace CrudService.Application.UseCases.Tickets.UpdateTicketStatus;

public record UpdateTicketStatusCommand(long Id, string NewStatus, string? Reason = null);
