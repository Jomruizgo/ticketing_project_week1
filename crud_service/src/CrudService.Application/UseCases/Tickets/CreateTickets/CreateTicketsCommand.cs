namespace CrudService.Application.UseCases.Tickets.CreateTickets;

public record CreateTicketsCommand(long EventId, int Quantity);
