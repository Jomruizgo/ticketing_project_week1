namespace CrudService.Application.UseCases.Waitlist.AssignOpportunity;

public record AssignOpportunityCommand(long TicketId, long EventId);
