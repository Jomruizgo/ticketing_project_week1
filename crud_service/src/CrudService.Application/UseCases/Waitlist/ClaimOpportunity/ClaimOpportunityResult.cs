namespace CrudService.Application.UseCases.Waitlist.ClaimOpportunity;

using CrudService.Application.Dtos;

public enum ClaimOpportunityResultType
{
    Claimed,
    NotFound,
    Expired,
    Forbidden
}

public record ClaimOpportunityResult(ClaimOpportunityResultType Type, ClaimOpportunityResponse? Response = null);
