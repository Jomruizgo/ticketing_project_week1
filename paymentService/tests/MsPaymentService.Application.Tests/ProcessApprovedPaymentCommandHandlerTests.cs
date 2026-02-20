using Microsoft.Extensions.Logging;
using MsPaymentService.Application.Dtos;
using MsPaymentService.Application.Interfaces;
using MsPaymentService.Application.UseCases.ProcessApprovedPayment;
using MsPaymentService.Domain.Entities;
using MsPaymentService.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace MsPaymentService.Application.Tests;

public class ProcessApprovedPaymentCommandHandlerTests
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITicketStateService _stateService;
    private readonly ProcessApprovedPaymentCommandHandler _sut;

    public ProcessApprovedPaymentCommandHandlerTests()
    {
        _ticketRepository = Substitute.For<ITicketRepository>();
        _paymentRepository = Substitute.For<IPaymentRepository>();
        _stateService = Substitute.For<ITicketStateService>();
        var logger = Substitute.For<ILogger<ProcessApprovedPaymentCommandHandler>>();
        _sut = new ProcessApprovedPaymentCommandHandler(_ticketRepository, _paymentRepository, _stateService, logger);
    }

    private static ProcessApprovedPaymentCommand CreateCommand(long ticketId = 1) => new(
        TicketId: ticketId,
        EventId: 100,
        AmountCents: 5000,
        Currency: "USD",
        PaymentBy: "user@test.com",
        TransactionRef: "TXN-001",
        ApprovedAt: DateTime.UtcNow);

    [Fact]
    public async Task ApprovedPayment_TicketNotFound_ReturnsFailure()
    {
        var command = CreateCommand(ticketId: 99);
        _ticketRepository.GetByIdAsync(99).Returns((Ticket?)null);

        var result = await _sut.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.FailureReason!);
    }

    [Fact]
    public async Task ApprovedPayment_AlreadyPaid_ReturnsAlreadyProcessed()
    {
        var command = CreateCommand();
        _ticketRepository.GetByIdAsync(1).Returns(new Ticket { Id = 1, Status = TicketStatus.paid });

        var result = await _sut.HandleAsync(command);

        Assert.True(result.IsAlreadyProcessed);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ApprovedPayment_InvalidStatus_Available_ReturnsFailure()
    {
        var command = CreateCommand();
        _ticketRepository.GetByIdAsync(1).Returns(new Ticket { Id = 1, Status = TicketStatus.available });

        var result = await _sut.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid ticket status", result.FailureReason!);
    }

    [Fact]
    public async Task ApprovedPayment_TtlExpired_TransitionsToReleased()
    {
        var command = CreateCommand() with { ApprovedAt = DateTime.UtcNow };
        var ticket = new Ticket
        {
            Id = 1,
            Status = TicketStatus.reserved,
            ReservedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        _ticketRepository.GetByIdAsync(1).Returns(ticket);
        _stateService.TransitionToReleasedAsync(1, Arg.Any<string>()).Returns(true);

        var result = await _sut.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("TTL", result.FailureReason!);
        await _stateService.Received(1).TransitionToReleasedAsync(1, Arg.Is<string>(s => s.Contains("TTL")));
    }

    [Fact]
    public async Task ApprovedPayment_NullReservedAt_TransitionsToReleased()
    {
        var command = CreateCommand();
        var ticket = new Ticket { Id = 1, Status = TicketStatus.reserved, ReservedAt = null };
        _ticketRepository.GetByIdAsync(1).Returns(ticket);
        _stateService.TransitionToReleasedAsync(1, Arg.Any<string>()).Returns(true);

        var result = await _sut.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("TTL", result.FailureReason!);
    }

    [Fact]
    public async Task ApprovedPayment_HappyPath_CreatesPaymentAndTransitions()
    {
        var command = CreateCommand();
        var ticket = new Ticket
        {
            Id = 1,
            Status = TicketStatus.reserved,
            ReservedAt = DateTime.UtcNow.AddMinutes(-2)
        };
        _ticketRepository.GetByIdAsync(1).Returns(ticket);
        _paymentRepository.GetByTicketIdAsync(1).Returns((Payment?)null);
        _paymentRepository.CreateAsync(Arg.Any<Payment>()).Returns(ci => ci.Arg<Payment>());
        _stateService.TransitionToPaidAsync(1, "TXN-001").Returns(true);

        var result = await _sut.HandleAsync(command);

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
        var command = CreateCommand();
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

        var result = await _sut.HandleAsync(command);

        Assert.True(result.IsSuccess);
        await _paymentRepository.DidNotReceive().CreateAsync(Arg.Any<Payment>());
    }

    [Fact]
    public async Task ApprovedPayment_TransitionFails_ReturnsFailure()
    {
        var command = CreateCommand();
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

        var result = await _sut.HandleAsync(command);

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
