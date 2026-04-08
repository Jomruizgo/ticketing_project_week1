namespace CrudService.Application.UseCases.Waitlist.AssignOpportunity;

using CrudService.Domain.Entities;
using CrudService.Domain.Enums;
using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

public class AssignOpportunityHandler : IAssignOpportunityUseCase
{
    private readonly IWaitlistOpportunityRepository _opportunityRepo;
    private readonly IWaitlistEntryRepository _entryRepo;
    private readonly IEventRepository _eventRepo;
    private readonly IPrioritizationStrategy _strategy;
    private readonly ITicketReservationPort _reservationPort;
    private readonly IOpportunityObserver _observer;
    private readonly ILogger<AssignOpportunityHandler> _logger;

    private static readonly long DefaultTtlMs = long.TryParse(
        Environment.GetEnvironmentVariable("WAITLIST_OPPORTUNITY_TTL_MS"), out var ttl) ? ttl : 900_000;

    public AssignOpportunityHandler(
        IWaitlistOpportunityRepository opportunityRepo,
        IWaitlistEntryRepository entryRepo,
        IEventRepository eventRepo,
        IPrioritizationStrategy strategy,
        ITicketReservationPort reservationPort,
        IOpportunityObserver observer,
        ILogger<AssignOpportunityHandler>? logger = null)
    {
        _opportunityRepo = opportunityRepo;
        _entryRepo = entryRepo;
        _eventRepo = eventRepo;
        _strategy = strategy;
        _reservationPort = reservationPort;
        _observer = observer;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AssignOpportunityHandler>.Instance;
    }

    public async Task<AssignOpportunityResult> HandleAsync(AssignOpportunityCommand command)
    {
        // Step 0: Verify waitlist is still open (FR-013)
        var eventEntity = await _eventRepo.GetByIdAsync(command.EventId);
        if (eventEntity is null || eventEntity.StartsAt <= DateTime.UtcNow)
        {
            _logger.LogWarning(
                "Waitlist closed for EventId={EventId}. Event date reached or event not found.",
                command.EventId);
            return new AssignOpportunityResult(AssignOpportunityResultType.NoEligible, null);
        }

        // Step 1: Idempotency check (FR-012)
        var existingOpportunity = await _opportunityRepo.FindActiveByTicketIdAsync(command.TicketId);
        if (existingOpportunity is not null)
        {
            _logger.LogInformation(
                "Active opportunity already exists for TicketId={TicketId}. Skipping.",
                command.TicketId);
            return new AssignOpportunityResult(AssignOpportunityResultType.Assigned, existingOpportunity);
        }

        // Step 2: Load all active entries for the event
        var activeEntries = await _entryRepo.GetActiveEntriesByEventAsync(command.EventId);
        var remainingEntries = new List<WaitlistEntry>(activeEntries);

        var anyAttempted = false;

        // Step 3: Iterate through eligible entries
        while (remainingEntries.Count > 0)
        {
            var selected = _strategy.SelectNextEligible(remainingEntries);
            if (selected is null)
                break;

            anyAttempted = true;

            // Create opportunity as Pending (for traceability)
            var opportunity = new WaitlistOpportunity
            {
                WaitlistEntryId = selected.Id,
                TicketId = command.TicketId,
                WaitlistEntry = selected
            };
            await _opportunityRepo.AddAsync(opportunity);

            // Step 4: Try reservation
            var reserved = await _reservationPort.TryReserveForWaitlistAsync(
                command.TicketId, selected.BuyerEmail);

            if (reserved)
            {
                // Step 5: Success — transition to Active
                opportunity.TransitionTo(WaitlistOpportunityStatus.Active);
                opportunity.ExpiresAt = opportunity.ActivatedAt!.Value.AddMilliseconds(DefaultTtlMs);
                await _opportunityRepo.UpdateAsync(opportunity);
                await _entryRepo.UpdateStatusAsync(selected.Id, WaitlistEntryStatus.Consumed);
                await _observer.OnOpportunityActivatedAsync(opportunity);

                _logger.LogInformation(
                    "Opportunity assigned. OpportunityId={OpportunityId}, EntryId={EntryId}, TicketId={TicketId}",
                    opportunity.Id, selected.Id, command.TicketId);

                return new AssignOpportunityResult(AssignOpportunityResultType.Assigned, opportunity);
            }

            // Step 6: Failure — transition to Failed, remove from list, try next
            opportunity.TransitionTo(WaitlistOpportunityStatus.Failed);
            await _opportunityRepo.UpdateAsync(opportunity);
            remainingEntries.Remove(selected);

            _logger.LogWarning(
                "Reservation failed for EntryId={EntryId}, TicketId={TicketId}. Trying next eligible.",
                selected.Id, command.TicketId);
        }

        // Step 7: No eligible or all failed
        if (!anyAttempted)
        {
            _logger.LogInformation(
                "No eligible buyers for EventId={EventId}, TicketId={TicketId}.",
                command.EventId, command.TicketId);
            return new AssignOpportunityResult(AssignOpportunityResultType.NoEligible, null);
        }

        _logger.LogWarning(
            "All reservation attempts failed for EventId={EventId}, TicketId={TicketId}.",
            command.EventId, command.TicketId);
        return new AssignOpportunityResult(AssignOpportunityResultType.AllFailed, null);
    }
}
