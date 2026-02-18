using Microsoft.Extensions.Logging;
using MsPaymentService.Worker.Models.DTOs;
using MsPaymentService.Worker.Models.Entities;
using MsPaymentService.Worker.Models.Events;
using MsPaymentService.Worker.Repositories;
using MsPaymentService.Worker.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace MsPaymentService.Worker.Tests;

public class PaymentValidationServiceTests
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITicketStateService _stateService;
    private readonly PaymentValidationService _sut;

    public PaymentValidationServiceTests()
    {
        _ticketRepository = Substitute.For<ITicketRepository>();
        _paymentRepository = Substitute.For<IPaymentRepository>();
        _stateService = Substitute.For<ITicketStateService>();
        var logger = Substitute.For<ILogger<PaymentValidationService>>();
        _sut = new PaymentValidationService(_ticketRepository, _paymentRepository, _stateService, logger);
    }

    private static PaymentApprovedEvent CreateApprovedEvent(long ticketId = 1) => new()
    {
        TicketId = ticketId,
        EventId = 100,
        AmountCents = 5000,
        Currency = "USD",
        PaymentBy = "user@test.com",
        TransactionRef = "TXN-001",
        ApprovedAt = DateTime.UtcNow
    };

    private static PaymentRejectedEvent CreateRejectedEvent(long ticketId = 1) => new()
    {
        TicketId = ticketId,
        PaymentId = 10,
        RejectionReason = "Insufficient funds",
        RejectedAt = DateTime.UtcNow,
        EventId = 100,
        EventTimestamp = DateTime.UtcNow
    };

    // === ValidateAndProcessApprovedPaymentAsync ===

    [Fact]
    public async Task ApprovedPayment_TicketNotFound_ReturnsFailure()
    {
        var evt = CreateApprovedEvent(ticketId: 99);
        _ticketRepository.GetByIdAsync(99).Returns((Ticket?)null);

        var result = await _sut.ValidateAndProcessApprovedPaymentAsync(evt);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.FailureReason!);
    }

    [Fact]
    public async Task ApprovedPayment_AlreadyPaid_ReturnsAlreadyProcessed()
    {
        var evt = CreateApprovedEvent();
        _ticketRepository.GetByIdAsync(1).Returns(new Ticket { Id = 1, Status = TicketStatus.paid });

        var result = await _sut.ValidateAndProcessApprovedPaymentAsync(evt);

        Assert.True(result.IsAlreadyProcessed);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ApprovedPayment_InvalidStatus_Available_ReturnsFailure()
    {
        var evt = CreateApprovedEvent();
        _ticketRepository.GetByIdAsync(1).Returns(new Ticket { Id = 1, Status = TicketStatus.available });

        var result = await _sut.ValidateAndProcessApprovedPaymentAsync(evt);

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid ticket status", result.FailureReason!);
    }

    [Fact]
    public async Task ApprovedPayment_TtlExpired_TransitionsToReleased()
    {
        var evt = CreateApprovedEvent();
        evt.ApprovedAt = DateTime.UtcNow;

        var ticket = new Ticket
        {
            Id = 1,
            Status = TicketStatus.reserved,
            ReservedAt = DateTime.UtcNow.AddMinutes(-10) // 10 minutes ago, well past 5-min TTL
        };
        _ticketRepository.GetByIdAsync(1).Returns(ticket);
        _stateService.TransitionToReleasedAsync(1, Arg.Any<string>()).Returns(true);

        var result = await _sut.ValidateAndProcessApprovedPaymentAsync(evt);

        Assert.False(result.IsSuccess);
        Assert.Contains("TTL", result.FailureReason!);
        await _stateService.Received(1).TransitionToReleasedAsync(1, Arg.Is<string>(s => s.Contains("TTL")));
    }

    [Fact]
    public async Task ApprovedPayment_NullReservedAt_TransitionsToReleased()
    {
        var evt = CreateApprovedEvent();
        var ticket = new Ticket { Id = 1, Status = TicketStatus.reserved, ReservedAt = null };
        _ticketRepository.GetByIdAsync(1).Returns(ticket);
        _stateService.TransitionToReleasedAsync(1, Arg.Any<string>()).Returns(true);

        var result = await _sut.ValidateAndProcessApprovedPaymentAsync(evt);

        Assert.False(result.IsSuccess);
        Assert.Contains("TTL", result.FailureReason!);
    }

    [Fact]
    public async Task ApprovedPayment_HappyPath_CreatesPaymentAndTransitions()
    {
        var evt = CreateApprovedEvent();
        var ticket = new Ticket
        {
            Id = 1,
            Status = TicketStatus.reserved,
            ReservedAt = DateTime.UtcNow.AddMinutes(-2) // within 5-min TTL
        };
        _ticketRepository.GetByIdAsync(1).Returns(ticket);
        _paymentRepository.GetByTicketIdAsync(1).Returns((Payment?)null);
        _paymentRepository.CreateAsync(Arg.Any<Payment>()).Returns(ci => ci.Arg<Payment>());
        _stateService.TransitionToPaidAsync(1, "TXN-001").Returns(true);

        var result = await _sut.ValidateAndProcessApprovedPaymentAsync(evt);

        Assert.True(result.IsSuccess);
        await _paymentRepository.Received(1).CreateAsync(Arg.Is<Payment>(p =>
            p.TicketId == 1 &&
            p.AmountCents == 5000 &&
            p.Currency == "USD" &&
            p.ProviderRef == "TXN-001"));
        await _stateService.Received(1).TransitionToPaidAsync(1, "TXN-001");
    }

    [Fact]
    public async Task ApprovedPayment_ExistingPayment_DoesNotCreateNew()
    {
        var evt = CreateApprovedEvent();
        var ticket = new Ticket
        {
            Id = 1,
            Status = TicketStatus.reserved,
            ReservedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        var existingPayment = new Payment { Id = 10, TicketId = 1 };
        _ticketRepository.GetByIdAsync(1).Returns(ticket);
        _paymentRepository.GetByTicketIdAsync(1).Returns(existingPayment);
        _stateService.TransitionToPaidAsync(1, "TXN-001").Returns(true);

        var result = await _sut.ValidateAndProcessApprovedPaymentAsync(evt);

        Assert.True(result.IsSuccess);
        await _paymentRepository.DidNotReceive().CreateAsync(Arg.Any<Payment>());
    }

    [Fact]
    public async Task ApprovedPayment_TransitionFails_ReturnsFailure()
    {
        var evt = CreateApprovedEvent();
        var ticket = new Ticket
        {
            Id = 1,
            Status = TicketStatus.reserved,
            ReservedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        _ticketRepository.GetByIdAsync(1).Returns(ticket);
        _paymentRepository.GetByTicketIdAsync(1).Returns((Payment?)null);
        _paymentRepository.CreateAsync(Arg.Any<Payment>()).Returns(ci => ci.Arg<Payment>());
        _stateService.TransitionToPaidAsync(1, "TXN-001").Returns(false);

        var result = await _sut.ValidateAndProcessApprovedPaymentAsync(evt);

        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to transition", result.FailureReason!);
    }

    // === ValidateAndProcessRejectedPaymentAsync ===

    [Fact]
    public async Task RejectedPayment_TicketNotFound_ReturnsFailure()
    {
        var evt = CreateRejectedEvent(ticketId: 99);
        _ticketRepository.GetByIdAsync(99).Returns((Ticket?)null);

        var result = await _sut.ValidateAndProcessRejectedPaymentAsync(evt);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.FailureReason!);
    }

    [Fact]
    public async Task RejectedPayment_AlreadyReleased_ReturnsAlreadyProcessed()
    {
        var evt = CreateRejectedEvent();
        _ticketRepository.GetByIdAsync(1).Returns(new Ticket { Id = 1, Status = TicketStatus.released });

        var result = await _sut.ValidateAndProcessRejectedPaymentAsync(evt);

        Assert.True(result.IsAlreadyProcessed);
    }

    [Fact]
    public async Task RejectedPayment_HappyPath_TransitionsToReleased()
    {
        var evt = CreateRejectedEvent();
        _ticketRepository.GetByIdAsync(1).Returns(new Ticket { Id = 1, Status = TicketStatus.reserved });
        _stateService.TransitionToReleasedAsync(1, Arg.Any<string>()).Returns(true);

        var result = await _sut.ValidateAndProcessRejectedPaymentAsync(evt);

        Assert.True(result.IsSuccess);
        await _stateService.Received(1).TransitionToReleasedAsync(1,
            Arg.Is<string>(s => s.Contains("Insufficient funds")));
    }

    [Fact]
    public async Task RejectedPayment_TransitionFails_ReturnsFailure()
    {
        var evt = CreateRejectedEvent();
        _ticketRepository.GetByIdAsync(1).Returns(new Ticket { Id = 1, Status = TicketStatus.reserved });
        _stateService.TransitionToReleasedAsync(1, Arg.Any<string>()).Returns(false);

        var result = await _sut.ValidateAndProcessRejectedPaymentAsync(evt);

        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to transition", result.FailureReason!);
    }

    // === IsWithinTimeLimit ===

    [Fact]
    public void IsWithinTimeLimit_WithinWindow_ReturnsTrue()
    {
        var reservedAt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var paymentAt = new DateTime(2025, 1, 1, 12, 4, 59, DateTimeKind.Utc);

        Assert.True(_sut.IsWithinTimeLimit(reservedAt, paymentAt));
    }

    [Fact]
    public void IsWithinTimeLimit_ExactBoundary_ReturnsTrue()
    {
        var reservedAt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var paymentAt = new DateTime(2025, 1, 1, 12, 5, 0, DateTimeKind.Utc);

        Assert.True(_sut.IsWithinTimeLimit(reservedAt, paymentAt));
    }

    [Fact]
    public void IsWithinTimeLimit_PastWindow_ReturnsFalse()
    {
        var reservedAt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var paymentAt = new DateTime(2025, 1, 1, 12, 5, 1, DateTimeKind.Utc);

        Assert.False(_sut.IsWithinTimeLimit(reservedAt, paymentAt));
    }
}
