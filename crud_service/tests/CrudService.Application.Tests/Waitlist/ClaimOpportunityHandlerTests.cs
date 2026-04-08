using CrudService.Application.UseCases.Waitlist.ClaimOpportunity;
using CrudService.Domain.Entities;
using CrudService.Domain.Enums;
using CrudService.Domain.Interfaces;
using NSubstitute;

namespace CrudService.Application.Tests.Waitlist;

public class ClaimOpportunityHandlerTests
{
    private readonly IWaitlistOpportunityRepository _opportunityRepo;
    private readonly ClaimOpportunityHandler _handler;

    public ClaimOpportunityHandlerTests()
    {
        _opportunityRepo = Substitute.For<IWaitlistOpportunityRepository>();
        _handler = new ClaimOpportunityHandler(_opportunityRepo);
    }

    [Fact]
    public async Task ActiveOpportunity_WithMatchingEmail_TransitionsToConsumed()
    {
        var entry = new WaitlistEntry
        {
            Id = 1, EventId = 42, BuyerEmail = "buyer@test.com",
            Status = WaitlistEntryStatus.Active
        };
        var opportunity = new WaitlistOpportunity
        {
            Id = 5, WaitlistEntryId = 1, TicketId = 100,
            Status = WaitlistOpportunityStatus.Active,
            ActivatedAt = DateTime.UtcNow.AddMinutes(-3),
            ExpiresAt = DateTime.UtcNow.AddMinutes(12),
            WaitlistEntry = entry, Ticket = new Ticket { Id = 100, EventId = 42 }
        };
        _opportunityRepo.FindByIdAsync(5).Returns(opportunity);

        var command = new ClaimOpportunityCommand(5, "buyer@test.com");
        var result = await _handler.HandleAsync(command);

        Assert.Equal(ClaimOpportunityResultType.Claimed, result.Type);
        Assert.NotNull(result.Response);
        Assert.Equal(5, result.Response!.OpportunityId);
        Assert.Equal(100, result.Response.TicketId);
        Assert.Equal(42, result.Response.EventId);
        Assert.Equal("consumed", result.Response.Status);
        Assert.Equal(WaitlistOpportunityStatus.Consumed, opportunity.Status);
        await _opportunityRepo.Received(1).UpdateAsync(opportunity);
    }

    [Fact]
    public async Task OpportunityNotFound_ReturnsNotFound()
    {
        _opportunityRepo.FindByIdAsync(999).Returns((WaitlistOpportunity?)null);

        var command = new ClaimOpportunityCommand(999, "buyer@test.com");
        var result = await _handler.HandleAsync(command);

        Assert.Equal(ClaimOpportunityResultType.NotFound, result.Type);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task ExpiredOpportunity_ReturnsExpired()
    {
        var entry = new WaitlistEntry
        {
            Id = 1, EventId = 42, BuyerEmail = "buyer@test.com",
            Status = WaitlistEntryStatus.Active
        };
        var opportunity = new WaitlistOpportunity
        {
            Id = 5, WaitlistEntryId = 1, TicketId = 100,
            Status = WaitlistOpportunityStatus.Active,
            ActivatedAt = DateTime.UtcNow.AddMinutes(-20),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5),
            WaitlistEntry = entry, Ticket = new Ticket { Id = 100, EventId = 42 }
        };
        _opportunityRepo.FindByIdAsync(5).Returns(opportunity);

        var command = new ClaimOpportunityCommand(5, "buyer@test.com");
        var result = await _handler.HandleAsync(command);

        Assert.Equal(ClaimOpportunityResultType.Expired, result.Type);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task WrongBuyerEmail_ReturnsForbidden()
    {
        var entry = new WaitlistEntry
        {
            Id = 1, EventId = 42, BuyerEmail = "owner@test.com",
            Status = WaitlistEntryStatus.Active
        };
        var opportunity = new WaitlistOpportunity
        {
            Id = 5, WaitlistEntryId = 1, TicketId = 100,
            Status = WaitlistOpportunityStatus.Active,
            ActivatedAt = DateTime.UtcNow.AddMinutes(-3),
            ExpiresAt = DateTime.UtcNow.AddMinutes(12),
            WaitlistEntry = entry, Ticket = new Ticket { Id = 100, EventId = 42 }
        };
        _opportunityRepo.FindByIdAsync(5).Returns(opportunity);

        var command = new ClaimOpportunityCommand(5, "intruder@test.com");
        var result = await _handler.HandleAsync(command);

        Assert.Equal(ClaimOpportunityResultType.Forbidden, result.Type);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task AlreadyConsumedOpportunity_ReturnsExpired()
    {
        var entry = new WaitlistEntry
        {
            Id = 1, EventId = 42, BuyerEmail = "buyer@test.com",
            Status = WaitlistEntryStatus.Consumed
        };
        var opportunity = new WaitlistOpportunity
        {
            Id = 5, WaitlistEntryId = 1, TicketId = 100,
            Status = WaitlistOpportunityStatus.Consumed,
            ActivatedAt = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            WaitlistEntry = entry, Ticket = new Ticket { Id = 100, EventId = 42 }
        };
        _opportunityRepo.FindByIdAsync(5).Returns(opportunity);

        var command = new ClaimOpportunityCommand(5, "buyer@test.com");
        var result = await _handler.HandleAsync(command);

        Assert.Equal(ClaimOpportunityResultType.Expired, result.Type);
        Assert.Null(result.Response);
    }
}
