using CrudService.Application.UseCases.Waitlist.AssignOpportunity;
using CrudService.Domain.Enums;
using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CrudService.Application.UseCases.Waitlist.ExpireOpportunity;

public class ExpireOpportunityHandler : IExpireOpportunityUseCase
{
    private readonly IWaitlistOpportunityRepository _opportunityRepo;
    private readonly IEnumerable<IOpportunityObserver> _observers;
    private readonly IAssignOpportunityUseCase _assignUseCase;
    private readonly IInventoryReturnPort _inventoryReturnPort;
    private readonly ILogger<ExpireOpportunityHandler> _logger;

    public ExpireOpportunityHandler(
        IWaitlistOpportunityRepository opportunityRepo,
        IEnumerable<IOpportunityObserver> observers,
        IAssignOpportunityUseCase assignUseCase,
        IInventoryReturnPort inventoryReturnPort,
        ILogger<ExpireOpportunityHandler> logger)
    {
        _opportunityRepo = opportunityRepo;
        _observers = observers;
        _assignUseCase = assignUseCase;
        _inventoryReturnPort = inventoryReturnPort;
        _logger = logger;
    }

    public async Task<ExpireOpportunityResult> HandleAsync(ExpireOpportunityCommand command)
    {
        var opportunity = await _opportunityRepo.FindByIdAsync(command.OpportunityId);

        if (opportunity is null)
        {
            _logger.LogWarning("Opportunity {OpportunityId} not found", command.OpportunityId);
            return new ExpireOpportunityResult(ExpireOpportunityResultType.NotFound);
        }

        if (opportunity.Status == WaitlistOpportunityStatus.Expired)
        {
            _logger.LogInformation("Opportunity {OpportunityId} already expired (idempotent)", command.OpportunityId);
            return new ExpireOpportunityResult(ExpireOpportunityResultType.AlreadyExpired);
        }

        if (opportunity.Status == WaitlistOpportunityStatus.Consumed)
        {
            _logger.LogInformation("Opportunity {OpportunityId} already consumed (idempotent)", command.OpportunityId);
            return new ExpireOpportunityResult(ExpireOpportunityResultType.AlreadyConsumed);
        }

        opportunity.TransitionTo(WaitlistOpportunityStatus.Expired);
        opportunity.ExpiredAt = DateTime.UtcNow;
        opportunity.ExpirationReason = "ttl_expired";

        await _opportunityRepo.UpdateAsync(opportunity);

        foreach (var observer in _observers)
        {
            await observer.OnOpportunityExpiredAsync(opportunity);
        }

        _logger.LogInformation(
            "Opportunity {OpportunityId} expired. Attempting reassignment for TicketId={TicketId}, EventId={EventId}",
            opportunity.Id, opportunity.TicketId, opportunity.WaitlistEntry.EventId);

        var reassignCommand = new AssignOpportunityCommand(opportunity.TicketId, opportunity.WaitlistEntry.EventId);
        var reassignResult = await _assignUseCase.HandleAsync(reassignCommand);

        if (reassignResult.Type is AssignOpportunityResultType.NoEligible or AssignOpportunityResultType.AllFailed)
        {
            try
            {
                await _inventoryReturnPort.ReturnToInventoryAsync(opportunity.TicketId, opportunity.WaitlistEntry.EventId);
                _logger.LogInformation(
                    "Returned ticket to inventory. TicketId={TicketId}, EventId={EventId}, Reason={Reason}",
                    opportunity.TicketId, opportunity.WaitlistEntry.EventId, reassignResult.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to return ticket to inventory. TicketId={TicketId}, EventId={EventId}. " +
                    "Opportunity already expired (consistent state). Manual intervention may be needed.",
                    opportunity.TicketId, opportunity.WaitlistEntry.EventId);
            }
        }

        return new ExpireOpportunityResult(ExpireOpportunityResultType.Expired);
    }
}
