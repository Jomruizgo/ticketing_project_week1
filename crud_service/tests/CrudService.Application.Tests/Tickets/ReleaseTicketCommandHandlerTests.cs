using CrudService.Application.UseCases.Tickets.ReleaseTicket;
using CrudService.Domain.Entities;
using CrudService.Domain.Exceptions;
using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CrudService.Application.Tests.Tickets;

public class ReleaseTicketCommandHandlerTests
{
    private readonly ITicketRepository _ticketRepo;
    private readonly ITicketHistoryRepository _historyRepo;
    private readonly ReleaseTicketCommandHandler _sut;

    public ReleaseTicketCommandHandlerTests()
    {
        _ticketRepo = Substitute.For<ITicketRepository>();
        _historyRepo = Substitute.For<ITicketHistoryRepository>();
        var logger = Substitute.For<ILogger<ReleaseTicketCommandHandler>>();
        _sut = new ReleaseTicketCommandHandler(_ticketRepo, _historyRepo, logger);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ThrowsTicketNotFoundException()
    {
        _ticketRepo.GetByIdAsync(999).Returns((Ticket?)null);
        await Assert.ThrowsAsync<TicketNotFoundException>(
            () => _sut.HandleAsync(new ReleaseTicketCommand(999)));
    }

    [Fact]
    public async Task HandleAsync_ClearsReservationFields()
    {
        // Arrange
        var ticket = new Ticket
        {
            Id = 1, EventId = 10, Status = TicketStatus.Reserved,
            ReservedAt = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            ReservedBy = "user@test.com", OrderId = "ORD-123", Version = 1
        };
        _ticketRepo.GetByIdAsync(1).Returns(ticket);
        _ticketRepo.UpdateAsync(Arg.Any<Ticket>()).Returns(callInfo => callInfo.Arg<Ticket>());

        // Act
        var result = await _sut.HandleAsync(new ReleaseTicketCommand(1, "Expirado"));

        // Assert
        Assert.Equal("Available", result.Status);
        Assert.Null(result.ReservedAt);
        Assert.Null(result.ExpiresAt);
        Assert.Null(result.ReservedBy);
        Assert.Null(result.OrderId);
        Assert.Equal(2, result.Version);
    }

    [Fact]
    public async Task HandleAsync_NoReason_UsesDefaultReason()
    {
        // Arrange
        var ticket = new Ticket { Id = 1, EventId = 10, Status = TicketStatus.Reserved, Version = 0 };
        _ticketRepo.GetByIdAsync(1).Returns(ticket);
        _ticketRepo.UpdateAsync(Arg.Any<Ticket>()).Returns(callInfo => callInfo.Arg<Ticket>());

        // Act
        await _sut.HandleAsync(new ReleaseTicketCommand(1));

        // Assert
        await _historyRepo.Received(1).AddAsync(Arg.Is<TicketHistory>(h =>
            h.Reason == "Ticket liberado"));
    }
}
