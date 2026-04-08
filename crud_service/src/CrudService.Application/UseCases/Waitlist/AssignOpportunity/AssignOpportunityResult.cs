namespace CrudService.Application.UseCases.Waitlist.AssignOpportunity;

using CrudService.Domain.Entities;

public enum AssignOpportunityResultType
{
    Assigned,
    NoEligible,
    AllFailed
}

public record AssignOpportunityResult(AssignOpportunityResultType Type, WaitlistOpportunity? Opportunity);
