namespace CrudService.Application.UseCases.Waitlist.ExpireOpportunity;

public interface IExpireOpportunityUseCase
{
    Task<ExpireOpportunityResult> HandleAsync(ExpireOpportunityCommand command);
}
