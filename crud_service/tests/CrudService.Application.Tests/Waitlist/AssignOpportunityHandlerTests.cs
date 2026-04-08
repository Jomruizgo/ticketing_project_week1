using CrudService.Application.UseCases.Waitlist.AssignOpportunity;
using CrudService.Domain.Entities;
using CrudService.Domain.Enums;
using CrudService.Domain.Events;
using CrudService.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace CrudService.Application.Tests.Waitlist;

public class AssignOpportunityHandlerTests
{
    private readonly IWaitlistOpportunityRepository _opportunityRepo;
    private readonly IWaitlistEntryRepository _entryRepo;
    private readonly IEventRepository _eventRepo;
    private readonly IPrioritizationStrategy _strategy;
    private readonly ITicketReservationPort _reservationPort;
    private readonly IOpportunityObserver _observer;
    private readonly AssignOpportunityHandler _handler;

    public AssignOpportunityHandlerTests()
    {
        _opportunityRepo = Substitute.For<IWaitlistOpportunityRepository>();
        _entryRepo = Substitute.For<IWaitlistEntryRepository>();
        _eventRepo = Substitute.For<IEventRepository>();
        _strategy = Substitute.For<IPrioritizationStrategy>();
        _reservationPort = Substitute.For<ITicketReservationPort>();
        _observer = Substitute.For<IOpportunityObserver>();

        _handler = new AssignOpportunityHandler(
            _opportunityRepo,
            _entryRepo,
            _eventRepo,
            _strategy,
            _reservationPort,
            new[] { _observer });
    }

    [Fact]
    public async Task AssignOpportunity_EligibleBuyerAndReservationSuccess_CreatesActiveOpportunity()
    {
        // Arrange
        var entry = new WaitlistEntry
        {
            Id = 1,
            EventId = 42,
            BuyerEmail = "buyer@example.com",
            Status = WaitlistEntryStatus.Active,
            EnrolledAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var futureEvent = new Event
        {
            Id = 42,
            Name = "Concert",
            StartsAt = DateTime.UtcNow.AddDays(7)
        };

        var entries = new List<WaitlistEntry> { entry };

        _eventRepo.GetByIdAsync(42).Returns(futureEvent);
        _opportunityRepo.FindActiveByTicketIdAsync(100).Returns((WaitlistOpportunity?)null);
        _entryRepo.GetActiveEntriesByEventAsync(42).Returns(entries);
        _strategy.SelectNextEligible(Arg.Any<IReadOnlyList<WaitlistEntry>>()).Returns(entry);
        _reservationPort.TryReserveForWaitlistAsync(100, "buyer@example.com").Returns(true);
        _opportunityRepo.AddAsync(Arg.Any<WaitlistOpportunity>()).Returns(ci =>
        {
            var opp = ci.Arg<WaitlistOpportunity>();
            opp.Id = 5;
            return opp;
        });

        // Act
        var result = await _handler.HandleAsync(new AssignOpportunityCommand(100, 42));

        // Assert
        Assert.Equal(AssignOpportunityResultType.Assigned, result.Type);
        Assert.NotNull(result.Opportunity);
        Assert.Equal(WaitlistOpportunityStatus.Active, result.Opportunity!.Status);
        Assert.NotNull(result.Opportunity.ActivatedAt);
        Assert.NotNull(result.Opportunity.ExpiresAt);
        Assert.True(result.Opportunity.ExpiresAt > result.Opportunity.ActivatedAt);

        await _entryRepo.Received(1).UpdateStatusAsync(1, WaitlistEntryStatus.Consumed);
        await _observer.Received(1).OnOpportunityActivatedAsync(
            Arg.Is<OpportunityActivatedEvent>(e =>
                e.EventName == "Concert" &&
                e.BuyerEmail == "buyer@example.com" &&
                e.EventId == 42));
    }

    [Fact]
    public async Task AssignOpportunity_MultipleBuyers_AssignsToOldest()
    {
        // Arrange
        var oldEntry = new WaitlistEntry
        {
            Id = 1, EventId = 42, BuyerEmail = "oldest@example.com",
            Status = WaitlistEntryStatus.Active,
            EnrolledAt = DateTime.UtcNow.AddMinutes(-30)
        };
        var midEntry = new WaitlistEntry
        {
            Id = 2, EventId = 42, BuyerEmail = "middle@example.com",
            Status = WaitlistEntryStatus.Active,
            EnrolledAt = DateTime.UtcNow.AddMinutes(-20)
        };
        var newEntry = new WaitlistEntry
        {
            Id = 3, EventId = 42, BuyerEmail = "newest@example.com",
            Status = WaitlistEntryStatus.Active,
            EnrolledAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var entries = new List<WaitlistEntry> { oldEntry, midEntry, newEntry };

        _eventRepo.GetByIdAsync(42).Returns(new Event { Id = 42, Name = "Concert", StartsAt = DateTime.UtcNow.AddDays(7) });
        _opportunityRepo.FindActiveByTicketIdAsync(100).Returns((WaitlistOpportunity?)null);
        _entryRepo.GetActiveEntriesByEventAsync(42).Returns(entries);
        _strategy.SelectNextEligible(Arg.Any<IReadOnlyList<WaitlistEntry>>()).Returns(oldEntry);
        _reservationPort.TryReserveForWaitlistAsync(100, "oldest@example.com").Returns(true);
        _opportunityRepo.AddAsync(Arg.Any<WaitlistOpportunity>()).Returns(ci =>
        {
            var opp = ci.Arg<WaitlistOpportunity>();
            opp.Id = 5;
            return opp;
        });

        // Act
        var result = await _handler.HandleAsync(new AssignOpportunityCommand(100, 42));

        // Assert
        Assert.Equal(AssignOpportunityResultType.Assigned, result.Type);
        Assert.Equal(oldEntry.Id, result.Opportunity!.WaitlistEntryId);
        await _entryRepo.Received(1).UpdateStatusAsync(oldEntry.Id, WaitlistEntryStatus.Consumed);
    }

    [Fact]
    public async Task AssignOpportunity_NoEligibleBuyers_ReturnsNoEligible()
    {
        // Arrange
        _eventRepo.GetByIdAsync(42).Returns(new Event { Id = 42, Name = "Concert", StartsAt = DateTime.UtcNow.AddDays(7) });
        _opportunityRepo.FindActiveByTicketIdAsync(100).Returns((WaitlistOpportunity?)null);
        _entryRepo.GetActiveEntriesByEventAsync(42).Returns(new List<WaitlistEntry>());
        _strategy.SelectNextEligible(Arg.Any<IReadOnlyList<WaitlistEntry>>()).Returns((WaitlistEntry?)null);

        // Act
        var result = await _handler.HandleAsync(new AssignOpportunityCommand(100, 42));

        // Assert
        Assert.Equal(AssignOpportunityResultType.NoEligible, result.Type);
        Assert.Null(result.Opportunity);
        await _opportunityRepo.DidNotReceive().AddAsync(Arg.Any<WaitlistOpportunity>());
        await _observer.DidNotReceive().OnOpportunityActivatedAsync(Arg.Any<OpportunityActivatedEvent>());
    }

    [Fact]
    public async Task AssignOpportunity_ReservationFails_TransitionsToFailedAndTriesNext()
    {
        // Arrange
        var entry1 = new WaitlistEntry
        {
            Id = 1, EventId = 42, BuyerEmail = "first@example.com",
            Status = WaitlistEntryStatus.Active,
            EnrolledAt = DateTime.UtcNow.AddMinutes(-20)
        };
        var entry2 = new WaitlistEntry
        {
            Id = 2, EventId = 42, BuyerEmail = "second@example.com",
            Status = WaitlistEntryStatus.Active,
            EnrolledAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var entries = new List<WaitlistEntry> { entry1, entry2 };

        _eventRepo.GetByIdAsync(42).Returns(new Event { Id = 42, Name = "Concert", StartsAt = DateTime.UtcNow.AddDays(7) });
        _opportunityRepo.FindActiveByTicketIdAsync(100).Returns((WaitlistOpportunity?)null);
        _entryRepo.GetActiveEntriesByEventAsync(42).Returns(entries);

        // Strategy returns entry1 first (full list), then entry2 (list without entry1)
        _strategy.SelectNextEligible(Arg.Is<IReadOnlyList<WaitlistEntry>>(l => l.Count == 2)).Returns(entry1);
        _strategy.SelectNextEligible(Arg.Is<IReadOnlyList<WaitlistEntry>>(l => l.Count == 1)).Returns(entry2);

        _reservationPort.TryReserveForWaitlistAsync(100, "first@example.com").Returns(false);
        _reservationPort.TryReserveForWaitlistAsync(100, "second@example.com").Returns(true);

        var addCallCount = 0;
        _opportunityRepo.AddAsync(Arg.Any<WaitlistOpportunity>()).Returns(ci =>
        {
            var opp = ci.Arg<WaitlistOpportunity>();
            opp.Id = ++addCallCount;
            return opp;
        });

        // Act
        var result = await _handler.HandleAsync(new AssignOpportunityCommand(100, 42));

        // Assert
        Assert.Equal(AssignOpportunityResultType.Assigned, result.Type);

        // Two opportunities created (one Failed, one Active)
        await _opportunityRepo.Received(2).AddAsync(Arg.Any<WaitlistOpportunity>());
        await _opportunityRepo.Received(2).UpdateAsync(Arg.Any<WaitlistOpportunity>());

        // Only entry2 consumed
        await _entryRepo.Received(1).UpdateStatusAsync(entry2.Id, WaitlistEntryStatus.Consumed);
        await _entryRepo.DidNotReceive().UpdateStatusAsync(entry1.Id, Arg.Any<WaitlistEntryStatus>());
    }

    [Fact]
    public async Task AssignOpportunity_AllReservationsFail_ReturnsAllFailed()
    {
        // Arrange
        var entry1 = new WaitlistEntry
        {
            Id = 1, EventId = 42, BuyerEmail = "only@example.com",
            Status = WaitlistEntryStatus.Active,
            EnrolledAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var entries = new List<WaitlistEntry> { entry1 };

        _eventRepo.GetByIdAsync(42).Returns(new Event { Id = 42, Name = "Concert", StartsAt = DateTime.UtcNow.AddDays(7) });
        _opportunityRepo.FindActiveByTicketIdAsync(100).Returns((WaitlistOpportunity?)null);
        _entryRepo.GetActiveEntriesByEventAsync(42).Returns(entries);

        _strategy.SelectNextEligible(Arg.Is<IReadOnlyList<WaitlistEntry>>(l => l.Count == 1)).Returns(entry1);
        _strategy.SelectNextEligible(Arg.Is<IReadOnlyList<WaitlistEntry>>(l => l.Count == 0)).Returns((WaitlistEntry?)null);

        _reservationPort.TryReserveForWaitlistAsync(100, "only@example.com").Returns(false);

        _opportunityRepo.AddAsync(Arg.Any<WaitlistOpportunity>()).Returns(ci =>
        {
            var opp = ci.Arg<WaitlistOpportunity>();
            opp.Id = 1;
            return opp;
        });

        // Act
        var result = await _handler.HandleAsync(new AssignOpportunityCommand(100, 42));

        // Assert
        Assert.Equal(AssignOpportunityResultType.AllFailed, result.Type);
        Assert.Null(result.Opportunity);

        // Opportunity created as Pending then transitioned to Failed
        await _opportunityRepo.Received(1).AddAsync(Arg.Any<WaitlistOpportunity>());
        await _observer.DidNotReceive().OnOpportunityActivatedAsync(Arg.Any<OpportunityActivatedEvent>());
        await _entryRepo.DidNotReceive().UpdateStatusAsync(Arg.Any<long>(), Arg.Any<WaitlistEntryStatus>());
    }

    [Fact]
    public async Task AssignOpportunity_ActiveOpportunityAlreadyExistsForTicket_ReturnsAssignedWithoutCreating()
    {
        // Arrange
        var existingOpportunity = new WaitlistOpportunity
        {
            Id = 5, TicketId = 100, WaitlistEntryId = 1,
            ActivatedAt = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        _eventRepo.GetByIdAsync(42).Returns(new Event { Id = 42, Name = "Concert", StartsAt = DateTime.UtcNow.AddDays(7) });
        _opportunityRepo.FindActiveByTicketIdAsync(100).Returns(existingOpportunity);

        // Act
        var result = await _handler.HandleAsync(new AssignOpportunityCommand(100, 42));

        // Assert
        Assert.Equal(AssignOpportunityResultType.Assigned, result.Type);
        Assert.Same(existingOpportunity, result.Opportunity);

        // Should NOT call any further methods
        await _entryRepo.DidNotReceive().GetActiveEntriesByEventAsync(Arg.Any<long>());
        _strategy.DidNotReceive().SelectNextEligible(Arg.Any<IReadOnlyList<WaitlistEntry>>());
        await _reservationPort.DidNotReceive().TryReserveForWaitlistAsync(Arg.Any<long>(), Arg.Any<string>());
        await _observer.DidNotReceive().OnOpportunityActivatedAsync(Arg.Any<OpportunityActivatedEvent>());
    }

    [Fact]
    public async Task AssignOpportunity_WaitlistClosed_ReturnsNoEligible()
    {
        // Arrange — event date already passed (FR-013)
        _eventRepo.GetByIdAsync(42).Returns(new Event
        {
            Id = 42, Name = "Past Concert",
            StartsAt = DateTime.UtcNow.AddDays(-1)
        });

        // Act
        var result = await _handler.HandleAsync(new AssignOpportunityCommand(100, 42));

        // Assert
        Assert.Equal(AssignOpportunityResultType.NoEligible, result.Type);
        Assert.Null(result.Opportunity);

        await _opportunityRepo.DidNotReceive().FindActiveByTicketIdAsync(Arg.Any<long>());
        await _entryRepo.DidNotReceive().GetActiveEntriesByEventAsync(Arg.Any<long>());
    }
}
