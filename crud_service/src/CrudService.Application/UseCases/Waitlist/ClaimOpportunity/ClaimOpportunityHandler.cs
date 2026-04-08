namespace CrudService.Application.UseCases.Waitlist.ClaimOpportunity;

using CrudService.Application.Dtos;
using CrudService.Domain.Enums;
using CrudService.Domain.Interfaces;

public class ClaimOpportunityHandler : IClaimOpportunityUseCase
{
    private readonly IWaitlistOpportunityRepository _opportunityRepo;

    public ClaimOpportunityHandler(IWaitlistOpportunityRepository opportunityRepo)
    {
        _opportunityRepo = opportunityRepo;
    }

    public async Task<ClaimOpportunityResult> HandleAsync(ClaimOpportunityCommand command)
    {
        var opportunity = await _opportunityRepo.FindByIdAsync(command.OpportunityId);

        if (opportunity is null)
            return new ClaimOpportunityResult(ClaimOpportunityResultType.NotFound);

        if (opportunity.Status != WaitlistOpportunityStatus.Active)
            return new ClaimOpportunityResult(ClaimOpportunityResultType.Expired);

        if (!string.Equals(opportunity.WaitlistEntry.BuyerEmail, command.BuyerEmail, StringComparison.OrdinalIgnoreCase))
            return new ClaimOpportunityResult(ClaimOpportunityResultType.Forbidden);

        if (opportunity.ExpiresAt.HasValue && opportunity.ExpiresAt.Value <= DateTime.UtcNow)
            return new ClaimOpportunityResult(ClaimOpportunityResultType.Expired);

        opportunity.TransitionTo(WaitlistOpportunityStatus.Consumed);
        await _opportunityRepo.UpdateAsync(opportunity);

        var response = new ClaimOpportunityResponse
        {
            OpportunityId = opportunity.Id,
            TicketId = opportunity.TicketId,
            EventId = opportunity.WaitlistEntry.EventId,
            Status = "consumed"
        };

        return new ClaimOpportunityResult(ClaimOpportunityResultType.Claimed, response);
    }
}
