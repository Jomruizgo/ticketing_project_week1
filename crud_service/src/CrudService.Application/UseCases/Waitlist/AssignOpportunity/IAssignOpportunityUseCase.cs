namespace CrudService.Application.UseCases.Waitlist.AssignOpportunity;

public interface IAssignOpportunityUseCase
{
    Task<AssignOpportunityResult> HandleAsync(AssignOpportunityCommand command);
}
