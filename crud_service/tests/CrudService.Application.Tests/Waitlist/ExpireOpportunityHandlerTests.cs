using CrudService.Application.UseCases.Waitlist.AssignOpportunity;
using CrudService.Application.UseCases.Waitlist.ExpireOpportunity;
using CrudService.Domain.Entities;
using CrudService.Domain.Enums;
using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace CrudService.Application.Tests.Waitlist;

public class ExpireOpportunityHandlerTests
{
    private readonly IWaitlistOpportunityRepository _opportunityRepo;
    private readonly IOpportunityObserver _observer;
    private readonly IAssignOpportunityUseCase _assignUseCase;
    private readonly IInventoryReturnPort _inventoryReturnPort;
    private readonly ILogger<ExpireOpportunityHandler> _logger;
    private readonly ExpireOpportunityHandler _handler;

    public ExpireOpportunityHandlerTests()
    {
        _opportunityRepo = Substitute.For<IWaitlistOpportunityRepository>();
        _observer = Substitute.For<IOpportunityObserver>();
        _assignUseCase = Substitute.For<IAssignOpportunityUseCase>();
        _inventoryReturnPort = Substitute.For<IInventoryReturnPort>();
        _logger = Substitute.For<ILogger<ExpireOpportunityHandler>>();

        _handler = new ExpireOpportunityHandler(
            _opportunityRepo,
            new[] { _observer },
            _assignUseCase,
            _inventoryReturnPort,
            _logger);
    }

    // === US1: T014 — TC-HU6-01: active → expired ===
    [Fact]
    public async Task HandleAsync_ActiveOpportunity_TransitionsToExpiredWithReasonAndTimestamp()
    {
        // Arrange
        var opportunity = CreateActiveOpportunity();
        _opportunityRepo.FindByIdAsync(1L).Returns(opportunity);
        _assignUseCase.HandleAsync(Arg.Any<AssignOpportunityCommand>())
            .Returns(new AssignOpportunityResult(AssignOpportunityResultType.NoEligible, null));

        // Act
        var result = await _handler.HandleAsync(new ExpireOpportunityCommand(1L));

        // Assert
        Assert.Equal(ExpireOpportunityResultType.Expired, result.Type);
        Assert.Equal(WaitlistOpportunityStatus.Expired, opportunity.Status);
        Assert.NotNull(opportunity.ExpiredAt);
        Assert.Equal("ttl_expired", opportunity.ExpirationReason);
        await _opportunityRepo.Received(1).UpdateAsync(opportunity);
        await _observer.Received(1).OnOpportunityExpiredAsync(opportunity);
    }

    // === US1: T015 — Idempotency: already expired ===
    [Fact]
    public async Task HandleAsync_AlreadyExpiredOpportunity_ReturnsAlreadyExpired()
    {
        // Arrange
        var opportunity = CreateActiveOpportunity();
        opportunity.TransitionTo(WaitlistOpportunityStatus.Expired);
        _opportunityRepo.FindByIdAsync(1L).Returns(opportunity);

        // Act
        var result = await _handler.HandleAsync(new ExpireOpportunityCommand(1L));

        // Assert
        Assert.Equal(ExpireOpportunityResultType.AlreadyExpired, result.Type);
        await _opportunityRepo.DidNotReceive().UpdateAsync(Arg.Any<WaitlistOpportunity>());
        await _observer.DidNotReceive().OnOpportunityExpiredAsync(Arg.Any<WaitlistOpportunity>());
    }

    // === US1: T016 — Idempotency: consumed ===
    [Fact]
    public async Task HandleAsync_ConsumedOpportunity_ReturnsAlreadyConsumed()
    {
        // Arrange
        var opportunity = CreateActiveOpportunity();
        opportunity.TransitionTo(WaitlistOpportunityStatus.Consumed);
        _opportunityRepo.FindByIdAsync(1L).Returns(opportunity);

        // Act
        var result = await _handler.HandleAsync(new ExpireOpportunityCommand(1L));

        // Assert
        Assert.Equal(ExpireOpportunityResultType.AlreadyConsumed, result.Type);
        await _opportunityRepo.DidNotReceive().UpdateAsync(Arg.Any<WaitlistOpportunity>());
    }

    // === US1: T017 — Not found ===
    [Fact]
    public async Task HandleAsync_OpportunityNotFound_ReturnsNotFound()
    {
        // Arrange
        _opportunityRepo.FindByIdAsync(999L).Returns((WaitlistOpportunity?)null);

        // Act
        var result = await _handler.HandleAsync(new ExpireOpportunityCommand(999L));

        // Assert
        Assert.Equal(ExpireOpportunityResultType.NotFound, result.Type);
    }

    // === US2: T022 — TC-HU6-02: reassignment invoked ===
    [Fact]
    public async Task HandleAsync_AfterExpiration_InvokesAssignOpportunityWithCorrectArgs()
    {
        // Arrange
        var opportunity = CreateActiveOpportunity();
        _opportunityRepo.FindByIdAsync(1L).Returns(opportunity);
        _assignUseCase.HandleAsync(Arg.Any<AssignOpportunityCommand>())
            .Returns(new AssignOpportunityResult(AssignOpportunityResultType.Assigned, null));

        // Act
        await _handler.HandleAsync(new ExpireOpportunityCommand(1L));

        // Assert
        await _assignUseCase.Received(1).HandleAsync(
            Arg.Is<AssignOpportunityCommand>(c => c.TicketId == 100 && c.EventId == 42));
    }

    // === US2: T023 — Assigned → no inventory return ===
    [Fact]
    public async Task HandleAsync_ReassignmentAssigned_DoesNotReturnToInventory()
    {
        // Arrange
        var opportunity = CreateActiveOpportunity();
        _opportunityRepo.FindByIdAsync(1L).Returns(opportunity);
        _assignUseCase.HandleAsync(Arg.Any<AssignOpportunityCommand>())
            .Returns(new AssignOpportunityResult(AssignOpportunityResultType.Assigned, null));

        // Act
        await _handler.HandleAsync(new ExpireOpportunityCommand(1L));

        // Assert
        await _inventoryReturnPort.DidNotReceive().ReturnToInventoryAsync(Arg.Any<long>(), Arg.Any<long>());
    }

    // === US3: T025 — TC-HU6-03: NoEligible → return to inventory ===
    [Fact]
    public async Task HandleAsync_NoEligibleBuyer_ReturnsToInventory()
    {
        // Arrange
        var opportunity = CreateActiveOpportunity();
        _opportunityRepo.FindByIdAsync(1L).Returns(opportunity);
        _assignUseCase.HandleAsync(Arg.Any<AssignOpportunityCommand>())
            .Returns(new AssignOpportunityResult(AssignOpportunityResultType.NoEligible, null));

        // Act
        await _handler.HandleAsync(new ExpireOpportunityCommand(1L));

        // Assert
        await _inventoryReturnPort.Received(1).ReturnToInventoryAsync(100, 42);
    }

    // === US3: T026 — AllFailed → return to inventory ===
    [Fact]
    public async Task HandleAsync_AllFailed_ReturnsToInventory()
    {
        // Arrange
        var opportunity = CreateActiveOpportunity();
        _opportunityRepo.FindByIdAsync(1L).Returns(opportunity);
        _assignUseCase.HandleAsync(Arg.Any<AssignOpportunityCommand>())
            .Returns(new AssignOpportunityResult(AssignOpportunityResultType.AllFailed, null));

        // Act
        await _handler.HandleAsync(new ExpireOpportunityCommand(1L));

        // Assert
        await _inventoryReturnPort.Received(1).ReturnToInventoryAsync(100, 42);
    }

    // === US3: T027 — TC-HU6-04: entry status not modified ===
    [Fact]
    public async Task HandleAsync_AfterExpiration_DoesNotModifyWaitlistEntryStatus()
    {
        // Arrange
        var opportunity = CreateActiveOpportunity();
        var originalEntryStatus = opportunity.WaitlistEntry.Status;
        _opportunityRepo.FindByIdAsync(1L).Returns(opportunity);
        _assignUseCase.HandleAsync(Arg.Any<AssignOpportunityCommand>())
            .Returns(new AssignOpportunityResult(AssignOpportunityResultType.NoEligible, null));

        // Act
        await _handler.HandleAsync(new ExpireOpportunityCommand(1L));

        // Assert
        Assert.Equal(originalEntryStatus, opportunity.WaitlistEntry.Status);
    }

    // === US3: T028 — TC-HU6-05: inventory return failure → opportunity already expired (consistent state) ===
    [Fact]
    public async Task HandleAsync_InventoryReturnFails_OpportunityRemainsExpiredAndLogsError()
    {
        // Arrange
        var opportunity = CreateActiveOpportunity();
        _opportunityRepo.FindByIdAsync(1L).Returns(opportunity);
        _assignUseCase.HandleAsync(Arg.Any<AssignOpportunityCommand>())
            .Returns(new AssignOpportunityResult(AssignOpportunityResultType.NoEligible, null));
        _inventoryReturnPort.ReturnToInventoryAsync(Arg.Any<long>(), Arg.Any<long>())
            .ThrowsAsync(new InvalidOperationException("RabbitMQ connection failed"));

        // Act
        var result = await _handler.HandleAsync(new ExpireOpportunityCommand(1L));

        // Assert — opportunity is already expired (consistent state)
        Assert.Equal(ExpireOpportunityResultType.Expired, result.Type);
        Assert.Equal(WaitlistOpportunityStatus.Expired, opportunity.Status);
        Assert.NotNull(opportunity.ExpiredAt);
    }

    private static WaitlistOpportunity CreateActiveOpportunity()
    {
        var entry = new WaitlistEntry
        {
            Id = 10,
            EventId = 42,
            BuyerEmail = "buyer@example.com",
            Status = WaitlistEntryStatus.Consumed,
            EnrolledAt = DateTime.UtcNow.AddMinutes(-30)
        };

        var opportunity = new WaitlistOpportunity
        {
            Id = 1,
            WaitlistEntryId = 10,
            TicketId = 100,
            Status = WaitlistOpportunityStatus.Pending,
            WaitlistEntry = entry
        };

        opportunity.TransitionTo(WaitlistOpportunityStatus.Active);
        return opportunity;
    }
}
