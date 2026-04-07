using Microsoft.Extensions.Logging;
using NSubstitute;
using ReservationService.Application.UseCases.ProcessExpiration;
using ReservationService.Application.UseCases.ProcessExpiration;
using ReservationService.Domain.Entities;
using ReservationService.Domain.Interfaces;
using Xunit;

namespace ReservationService.Application.Tests;

public class ProcessExpirationCommandHandlerTests
{
    private readonly ITicketRepository _repository;
    private readonly ProcessExpirationCommandHandler _sut;

    public ProcessExpirationCommandHandlerTests()
    {
        _repository = Substitute.For<ITicketRepository>();
        var logger = Substitute.For<ILogger<ProcessExpirationCommandHandler>>();
        _sut = new ProcessExpirationCommandHandler(_repository, logger);
    }

    [Fact]
    public async Task HandleAsync_ReservedTicket_ReleasesAndReturnsStatusChanged()
    {
        var command = new ProcessExpirationCommand(1);
        var ticket = new Ticket { Id = 1, Status = TicketStatus.Reserved, Version = 3 };

        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ticket);
        _repository.TryReleaseAsync(ticket, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.HandleAsync(command);

        Assert.True(result.Success);
        Assert.True(result.StatusChanged);
        await _repository.Received(1).TryReleaseAsync(ticket, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PaidTicket_ReturnsNoOp()
    {
        var command = new ProcessExpirationCommand(2);
        _repository.GetByIdAsync(2, Arg.Any<CancellationToken>())
            .Returns(new Ticket { Id = 2, Status = TicketStatus.Paid });

        var result = await _sut.HandleAsync(command);

        Assert.True(result.Success);
        Assert.False(result.StatusChanged);
        await _repository.DidNotReceive().TryReleaseAsync(Arg.Any<Ticket>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AlreadyReleasedTicket_ReturnsIdempotentNoOp()
    {
        var command = new ProcessExpirationCommand(3);
        _repository.GetByIdAsync(3, Arg.Any<CancellationToken>())
            .Returns(new Ticket { Id = 3, Status = TicketStatus.Released });

        var result = await _sut.HandleAsync(command);

        Assert.True(result.Success);
        Assert.False(result.StatusChanged);
        await _repository.DidNotReceive().TryReleaseAsync(Arg.Any<Ticket>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ConcurrentModification_ReturnsFailure()
    {
        var command = new ProcessExpirationCommand(4);
        var ticket = new Ticket { Id = 4, Status = TicketStatus.Reserved, Version = 8 };

        _repository.GetByIdAsync(4, Arg.Any<CancellationToken>()).Returns(ticket);
        _repository.TryReleaseAsync(ticket, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.HandleAsync(command);

        Assert.False(result.Success);
        Assert.False(result.StatusChanged);
        Assert.Contains("modified by another process", result.ErrorMessage!);
    }

    [Fact]
    public async Task HandleAsync_RepositoryThrows_ReturnsTechnicalFailure()
    {
        var command = new ProcessExpirationCommand(5);
        var ticket = new Ticket { Id = 5, Status = TicketStatus.Reserved, Version = 2 };

        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(ticket);
        _repository
            .TryReleaseAsync(ticket, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<bool>(new Exception("db offline")));

        var result = await _sut.HandleAsync(command);

        Assert.False(result.Success);
        Assert.False(result.StatusChanged);
        Assert.Contains("technical error", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }
}
