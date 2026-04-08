namespace CrudService.Application.UseCases.Waitlist.ClaimOpportunity;

public interface IClaimOpportunityUseCase
{
    Task<ClaimOpportunityResult> HandleAsync(ClaimOpportunityCommand command);
}
