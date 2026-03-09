namespace CrudService.Application.UseCases.Tickets.ReleaseTicket;

public record ReleaseTicketCommand(long Id, string? Reason = null);
