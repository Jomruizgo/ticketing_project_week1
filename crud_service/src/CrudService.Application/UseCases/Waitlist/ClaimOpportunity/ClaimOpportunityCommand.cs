namespace CrudService.Application.UseCases.Waitlist.ClaimOpportunity;

public record ClaimOpportunityCommand(long OpportunityId, string BuyerEmail);
