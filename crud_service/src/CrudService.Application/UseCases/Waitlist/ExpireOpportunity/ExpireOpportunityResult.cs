namespace CrudService.Application.UseCases.Waitlist.ExpireOpportunity;

public enum ExpireOpportunityResultType
{
    Expired,
    AlreadyExpired,
    AlreadyConsumed,
    NotFound
}

public record ExpireOpportunityResult(ExpireOpportunityResultType Type);
