using Xunit;
using NSubstitute;
using Microsoft.Extensions.Logging;
using ReservationService.Worker.Models;
using ReservationService.Worker.Repositories;
using ReservationService.Worker.Services;

namespace ReservationService.Worker.Tests;

public class ReservationServiceImplTests
{
    private readonly ITicketRepository _repository;
    private readonly ReservationServiceImpl _sut;

    public ReservationServiceImplTests()
    {
        _repository = Substitute.For<ITicketRepository>();
        var logger = Substitute.For<ILogger<ReservationServiceImpl>>();
        _sut = new ReservationServiceImpl(_repository, logger);
    }

    private static ReservationMessage CreateMessage(long ticketId = 1) => new()
    {
        TicketId = ticketId,
        EventId = 100,
        OrderId = "order-123",
        ReservedBy = "user-456",
        ReservationDurationSeconds = 300,
        PublishedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task ProcessReservation_TicketNotFound_ReturnsFailure()
    {
        var message = CreateMessage(ticketId: 99);
        _repository.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns((Ticket?)null);

        var result = await _sut.ProcessReservationAsync(message);

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage!);
    }

    [Fact]
    public async Task ProcessReservation_TicketAlreadyReserved_ReturnsFailure()
    {
        var message = CreateMessage();
        var ticket = new Ticket { Id = 1, Status = TicketStatus.Reserved };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ticket);

        var result = await _sut.ProcessReservationAsync(message);

        Assert.False(result.Success);
        Assert.Contains("not available", result.ErrorMessage!);
    }

    [Fact]
    public async Task ProcessReservation_AvailableTicket_ReturnsSuccess()
    {
        var message = CreateMessage();
        var ticket = new Ticket { Id = 1, Status = TicketStatus.Available };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ticket);
        _repository.TryReserveAsync(ticket, "user-456", "order-123", Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.ProcessReservationAsync(message);

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ProcessReservation_ConcurrentModification_ReturnsFailure()
    {
        var message = CreateMessage();
        var ticket = new Ticket { Id = 1, Status = TicketStatus.Available };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ticket);
        _repository.TryReserveAsync(ticket, "user-456", "order-123", Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.ProcessReservationAsync(message);

        Assert.False(result.Success);
        Assert.Contains("modified by another process", result.ErrorMessage!);
    }
}
