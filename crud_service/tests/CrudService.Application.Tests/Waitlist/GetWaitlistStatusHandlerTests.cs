using CrudService.Application.UseCases.Waitlist.GetWaitlistStatus;
using CrudService.Domain.Entities;
using CrudService.Domain.Enums;
using CrudService.Domain.Interfaces;
using NSubstitute;

namespace CrudService.Application.Tests.Waitlist;

public class GetWaitlistStatusHandlerTests
{
    private readonly IWaitlistEntryRepository _waitlistEntryRepository;
    private readonly IWaitlistOpportunityRepository _waitlistOpportunityRepository;
    private readonly GetWaitlistStatusHandler _handler;

    public GetWaitlistStatusHandlerTests()
    {
        _waitlistEntryRepository = Substitute.For<IWaitlistEntryRepository>();
        _waitlistOpportunityRepository = Substitute.For<IWaitlistOpportunityRepository>();
        _handler = new GetWaitlistStatusHandler(_waitlistEntryRepository, _waitlistOpportunityRepository);
    }

    // --- TC-HU2-01: Ver inscripción activa ---

    [Fact(DisplayName = "TC-HU2-01: Returns entry with null opportunity when active entry exists without opportunity")]
    public async Task ReturnsEntryWithNullOpportunity_WhenActiveEntryExistsWithoutOpportunity()
    {
        var entry = new WaitlistEntry
        {
            Id = 1, EventId = 42, BuyerEmail = "buyer@test.com",
            Status = WaitlistEntryStatus.Active, EnrolledAt = DateTime.UtcNow.AddHours(-1)
        };
        _waitlistEntryRepository.FindActiveByEventAndEmailAsync(42, "buyer@test.com").Returns(entry);
        _waitlistOpportunityRepository.FindByWaitlistEntryIdAsync(1).Returns((WaitlistOpportunity?)null);

        var query = new GetWaitlistStatusQuery(42, "buyer@test.com");
        var result = await _handler.HandleAsync(query);

        Assert.NotNull(result);
        Assert.Equal(1, result.Entry.Id);
        Assert.Equal(42, result.Entry.EventId);
        Assert.Equal("buyer@test.com", result.Entry.BuyerEmail);
        Assert.Equal("active", result.Entry.Status);
        Assert.Null(result.Opportunity);
    }

    // --- TC-HU2-02: Ver oportunidad activa con tiempo restante ---

    [Fact(DisplayName = "TC-HU2-02: Returns opportunity with remaining minutes when opportunity is active")]
    public async Task ReturnsOpportunityWithRemainingMinutes_WhenOpportunityIsActive()
    {
        var entry = new WaitlistEntry
        {
            Id = 1, EventId = 42, BuyerEmail = "buyer@test.com",
            Status = WaitlistEntryStatus.Active, EnrolledAt = DateTime.UtcNow.AddHours(-1)
        };
        var opportunity = new WaitlistOpportunity
        {
            Id = 5, WaitlistEntryId = 1, TicketId = 100,
            Status = WaitlistOpportunityStatus.Active,
            ActivatedAt = DateTime.UtcNow.AddMinutes(-3),
            ExpiresAt = DateTime.UtcNow.AddMinutes(12)
        };
        _waitlistEntryRepository.FindActiveByEventAndEmailAsync(42, "buyer@test.com").Returns(entry);
        _waitlistOpportunityRepository.FindByWaitlistEntryIdAsync(1).Returns(opportunity);

        var query = new GetWaitlistStatusQuery(42, "buyer@test.com");
        var result = await _handler.HandleAsync(query);

        Assert.NotNull(result);
        Assert.NotNull(result.Opportunity);
        Assert.Equal("active", result.Opportunity.Status);
        Assert.Equal(100, result.Opportunity.TicketId);
        Assert.True(result.Opportunity.RemainingMinutes > 0);
        Assert.True(result.Opportunity.RemainingMinutes <= 12);
    }

    // --- TC-HU2-03: Ver oportunidad expirada ---

    [Fact(DisplayName = "TC-HU2-03a: Returns opportunity expired with zero remaining minutes")]
    public async Task ReturnsOpportunityExpired_WhenOpportunityStatusIsExpired()
    {
        var entry = new WaitlistEntry
        {
            Id = 1, EventId = 42, BuyerEmail = "buyer@test.com",
            Status = WaitlistEntryStatus.Active, EnrolledAt = DateTime.UtcNow.AddHours(-2)
        };
        var opportunity = new WaitlistOpportunity
        {
            Id = 5, WaitlistEntryId = 1, TicketId = 100,
            Status = WaitlistOpportunityStatus.Expired,
            ActivatedAt = DateTime.UtcNow.AddMinutes(-20),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
        };
        _waitlistEntryRepository.FindActiveByEventAndEmailAsync(42, "buyer@test.com").Returns(entry);
        _waitlistOpportunityRepository.FindByWaitlistEntryIdAsync(1).Returns(opportunity);

        var query = new GetWaitlistStatusQuery(42, "buyer@test.com");
        var result = await _handler.HandleAsync(query);

        Assert.NotNull(result);
        Assert.NotNull(result.Opportunity);
        Assert.Equal("expired", result.Opportunity.Status);
        Assert.Equal(0, result.Opportunity.RemainingMinutes);
    }

    // --- TC-HU2-03 (parcial): Ver oportunidad consumida ---

    [Fact(DisplayName = "TC-HU2-03b: Returns opportunity consumed with zero remaining minutes")]
    public async Task ReturnsOpportunityConsumed_WhenOpportunityStatusIsConsumed()
    {
        var entry = new WaitlistEntry
        {
            Id = 1, EventId = 42, BuyerEmail = "buyer@test.com",
            Status = WaitlistEntryStatus.Active, EnrolledAt = DateTime.UtcNow.AddHours(-2)
        };
        var opportunity = new WaitlistOpportunity
        {
            Id = 5, WaitlistEntryId = 1, TicketId = 100,
            Status = WaitlistOpportunityStatus.Consumed,
            ActivatedAt = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
        };
        _waitlistEntryRepository.FindActiveByEventAndEmailAsync(42, "buyer@test.com").Returns(entry);
        _waitlistOpportunityRepository.FindByWaitlistEntryIdAsync(1).Returns(opportunity);

        var query = new GetWaitlistStatusQuery(42, "buyer@test.com");
        var result = await _handler.HandleAsync(query);

        Assert.NotNull(result);
        Assert.NotNull(result.Opportunity);
        Assert.Equal("consumed", result.Opportunity.Status);
        Assert.Equal(0, result.Opportunity.RemainingMinutes);
    }

    // --- Inscripción no encontrada ---

    [Fact(DisplayName = "Returns null when no entry found")]
    public async Task ReturnsNull_WhenNoEntryFound()
    {
        _waitlistEntryRepository.FindActiveByEventAndEmailAsync(42, "nobody@test.com")
            .Returns((WaitlistEntry?)null);

        var query = new GetWaitlistStatusQuery(42, "nobody@test.com");
        var result = await _handler.HandleAsync(query);

        Assert.Null(result);
    }

    // --- TC-HU2-04: Distinción correcta entre los cuatro estados ---

    [Fact(DisplayName = "TC-HU2-04: Distinguishes all four visible states correctly")]
    public async Task DistinguishesAllFourStatesCorrectly()
    {
        // State 1: Active entry, no opportunity
        var entry1 = new WaitlistEntry
        {
            Id = 1, EventId = 42, BuyerEmail = "buyer@test.com",
            Status = WaitlistEntryStatus.Active, EnrolledAt = DateTime.UtcNow.AddHours(-1)
        };
        _waitlistEntryRepository.FindActiveByEventAndEmailAsync(42, "buyer@test.com").Returns(entry1);
        _waitlistOpportunityRepository.FindByWaitlistEntryIdAsync(1).Returns((WaitlistOpportunity?)null);

        var result1 = await _handler.HandleAsync(new GetWaitlistStatusQuery(42, "buyer@test.com"));
        Assert.NotNull(result1);
        Assert.Equal("active", result1.Entry.Status);
        Assert.Null(result1.Opportunity);

        // State 2: Active opportunity
        var opportunity2 = new WaitlistOpportunity
        {
            Id = 5, WaitlistEntryId = 1, TicketId = 100,
            Status = WaitlistOpportunityStatus.Active,
            ActivatedAt = DateTime.UtcNow.AddMinutes(-3), ExpiresAt = DateTime.UtcNow.AddMinutes(12)
        };
        _waitlistOpportunityRepository.FindByWaitlistEntryIdAsync(1).Returns(opportunity2);

        var result2 = await _handler.HandleAsync(new GetWaitlistStatusQuery(42, "buyer@test.com"));
        Assert.NotNull(result2);
        Assert.NotNull(result2.Opportunity);
        Assert.Equal("active", result2.Opportunity.Status);
        Assert.True(result2.Opportunity.RemainingMinutes > 0);

        // State 3: Consumed opportunity
        var opportunity3 = new WaitlistOpportunity
        {
            Id = 5, WaitlistEntryId = 1, TicketId = 100,
            Status = WaitlistOpportunityStatus.Consumed,
            ActivatedAt = DateTime.UtcNow.AddMinutes(-10), ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
        };
        _waitlistOpportunityRepository.FindByWaitlistEntryIdAsync(1).Returns(opportunity3);

        var result3 = await _handler.HandleAsync(new GetWaitlistStatusQuery(42, "buyer@test.com"));
        Assert.NotNull(result3);
        Assert.NotNull(result3.Opportunity);
        Assert.Equal("consumed", result3.Opportunity.Status);
        Assert.Equal(0, result3.Opportunity.RemainingMinutes);

        // State 4: Expired opportunity
        var opportunity4 = new WaitlistOpportunity
        {
            Id = 5, WaitlistEntryId = 1, TicketId = 100,
            Status = WaitlistOpportunityStatus.Expired,
            ActivatedAt = DateTime.UtcNow.AddMinutes(-20), ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
        };
        _waitlistOpportunityRepository.FindByWaitlistEntryIdAsync(1).Returns(opportunity4);

        var result4 = await _handler.HandleAsync(new GetWaitlistStatusQuery(42, "buyer@test.com"));
        Assert.NotNull(result4);
        Assert.NotNull(result4.Opportunity);
        Assert.Equal("expired", result4.Opportunity.Status);
        Assert.Equal(0, result4.Opportunity.RemainingMinutes);
    }
}
