namespace CrudService.Application.UseCases.Waitlist.GetWaitlistStatus;

using CrudService.Application.Dtos;
using CrudService.Domain.Enums;
using CrudService.Domain.Interfaces;

public class GetWaitlistStatusHandler : IGetWaitlistStatusUseCase
{
    private readonly IWaitlistEntryRepository _waitlistEntryRepository;
    private readonly IWaitlistOpportunityRepository _waitlistOpportunityRepository;

    public GetWaitlistStatusHandler(
        IWaitlistEntryRepository waitlistEntryRepository,
        IWaitlistOpportunityRepository waitlistOpportunityRepository)
    {
        _waitlistEntryRepository = waitlistEntryRepository;
        _waitlistOpportunityRepository = waitlistOpportunityRepository;
    }

    public async Task<WaitlistStatusResponse?> HandleAsync(GetWaitlistStatusQuery query)
    {
        var normalizedEmail = query.BuyerEmail.ToLowerInvariant();

        var entry = await _waitlistEntryRepository.FindActiveByEventAndEmailAsync(
            query.EventId, normalizedEmail);

        if (entry is null)
            return null;

        var entryDto = new WaitlistEntryDto
        {
            Id = entry.Id,
            EventId = entry.EventId,
            BuyerEmail = entry.BuyerEmail,
            Status = entry.Status.ToString().ToLowerInvariant(),
            EnrolledAt = entry.EnrolledAt
        };

        var opportunity = await _waitlistOpportunityRepository.FindByWaitlistEntryIdAsync(entry.Id);

        WaitlistOpportunityDto? opportunityDto = null;
        if (opportunity is not null)
        {
            var remainingMinutes = opportunity.Status == WaitlistOpportunityStatus.Active
                                   && opportunity.ExpiresAt.HasValue
                ? Math.Max(0, (int)(opportunity.ExpiresAt.Value - DateTime.UtcNow).TotalMinutes)
                : 0;

            opportunityDto = new WaitlistOpportunityDto
            {
                Id = opportunity.Id,
                TicketId = opportunity.TicketId,
                Status = opportunity.Status.ToString().ToLowerInvariant(),
                ActivatedAt = opportunity.ActivatedAt,
                ExpiresAt = opportunity.ExpiresAt,
                RemainingMinutes = remainingMinutes
            };
        }

        return new WaitlistStatusResponse
        {
            Entry = entryDto,
            Opportunity = opportunityDto
        };
    }
}
