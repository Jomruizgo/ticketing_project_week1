using CrudService.Application.Exceptions;
using CrudService.Application.UseCases.Tickets.UpdateTicketStatus;
using CrudService.Domain.Entities;
using CrudService.Domain.Exceptions;
using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CrudService.Application.Tests.Tickets;

public class UpdateTicketStatusCommandHandlerTests
{
    private readonly ITicketRepository _ticketRepo;
    private readonly ITicketHistoryRepository _historyRepo;
    private readonly UpdateTicketStatusCommandHandler _sut;

    public UpdateTicketStatusCommandHandlerTests()
    {
        _ticketRepo = Substitute.For<ITicketRepository>();
        _historyRepo = Substitute.For<ITicketHistoryRepository>();
        var logger = Substitute.For<ILogger<UpdateTicketStatusCommandHandler>>();
        _sut = new UpdateTicketStatusCommandHandler(_ticketRepo, _historyRepo, logger);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ThrowsTicketNotFoundException()
    {
        _ticketRepo.GetByIdAsync(999).Returns((Ticket?)null);
        await Assert.ThrowsAsync<TicketNotFoundException>(
            () => _sut.HandleAsync(new UpdateTicketStatusCommand(999, "Reserved")));
    }

    [Fact]
    public async Task HandleAsync_InvalidStatus_ThrowsInvalidTicketStatusException()
    {
        var ticket = new Ticket { Id = 1, EventId = 10, Status = TicketStatus.Available };
        _ticketRepo.GetByIdAsync(1).Returns(ticket);
        await Assert.ThrowsAsync<InvalidTicketStatusException>(
            () => _sut.HandleAsync(new UpdateTicketStatusCommand(1, "InvalidStatus")));
    }

    [Fact]
    public async Task HandleAsync_ValidTransition_UpdatesAndRecordsHistory()
    {
        // Arrange
        var ticket = new Ticket { Id = 1, EventId = 10, Status = TicketStatus.Available, Version = 0 };
        _ticketRepo.GetByIdAsync(1).Returns(ticket);
        _ticketRepo.UpdateAsync(Arg.Any<Ticket>()).Returns(callInfo => callInfo.Arg<Ticket>());

        // Act
        var result = await _sut.HandleAsync(new UpdateTicketStatusCommand(1, "Reserved", "Usuario reservó"));

        // Assert
        Assert.Equal("Reserved", result.Status);
        Assert.Equal(1, result.Version);
        await _historyRepo.Received(1).AddAsync(Arg.Is<TicketHistory>(h =>
            h.TicketId == 1 &&
            h.OldStatus == TicketStatus.Available &&
            h.NewStatus == TicketStatus.Reserved &&
            h.Reason == "Usuario reservó"));
    }
}
