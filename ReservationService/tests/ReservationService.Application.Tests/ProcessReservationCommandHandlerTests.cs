using Xunit;
using NSubstitute;
using Microsoft.Extensions.Logging;
using ReservationService.Application.UseCases.ProcessReservation;
using ReservationService.Domain.Entities;
using ReservationService.Domain.Interfaces;

namespace ReservationService.Application.Tests;

public class ProcessReservationCommandHandlerTests
{
    private readonly ITicketRepository _repository;
    private readonly ProcessReservationCommandHandler _sut;

    public ProcessReservationCommandHandlerTests()
    {
        _repository = Substitute.For<ITicketRepository>();
        var logger = Substitute.For<ILogger<ProcessReservationCommandHandler>>();
        _sut = new ProcessReservationCommandHandler(_repository, logger);
    }

    private static ProcessReservationCommand CreateCommand(long ticketId = 1) => new(
        TicketId: ticketId,
        EventId: 100,
        OrderId: "order-123",
        ReservedBy: "user-456",
        ReservationDurationSeconds: 300,
        PublishedAt: DateTime.UtcNow
    );

    [Fact]
    public async Task HandleAsync_TicketNotFound_ReturnsFailure()
    {
        var command = CreateCommand(ticketId: 99);
        _repository.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns((Ticket?)null);

        var result = await _sut.HandleAsync(command);

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage!);
    }

    [Fact]
    public async Task HandleAsync_TicketAlreadyReserved_ReturnsFailure()
    {
        var command = CreateCommand();
        var ticket = new Ticket { Id = 1, Status = TicketStatus.Reserved };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ticket);

        var result = await _sut.HandleAsync(command);

        Assert.False(result.Success);
        Assert.Contains("not available", result.ErrorMessage!);
    }

    [Fact]
    public async Task HandleAsync_AvailableTicket_ReturnsSuccess()
    {
        var command = CreateCommand();
        var ticket = new Ticket { Id = 1, Status = TicketStatus.Available };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ticket);
        _repository.TryReserveAsync(ticket, "user-456", "order-123", Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.HandleAsync(command);

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_ConcurrentModification_ReturnsFailure()
    {
        var command = CreateCommand();
        var ticket = new Ticket { Id = 1, Status = TicketStatus.Available };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(ticket);
        _repository.TryReserveAsync(ticket, "user-456", "order-123", Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.HandleAsync(command);

        Assert.False(result.Success);
        Assert.Contains("modified by another process", result.ErrorMessage!);
    }
}
