using CrudService.Application.UseCases.Tickets.CreateTickets;
using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CrudService.Application.Tests.Tickets;

public class CreateTicketsCommandHandlerTests
{
    private readonly ITicketRepository _ticketRepo;
    private readonly CreateTicketsCommandHandler _sut;

    public CreateTicketsCommandHandlerTests()
    {
        _ticketRepo = Substitute.For<ITicketRepository>();
        var logger = Substitute.For<ILogger<CreateTicketsCommandHandler>>();
        _sut = new CreateTicketsCommandHandler(_ticketRepo, logger);
    }

    [Fact]
    public async Task HandleAsync_CreatesRequestedQuantity()
    {
        // Arrange
        var callCount = 0;
        _ticketRepo.AddAsync(Arg.Any<Ticket>()).Returns(callInfo =>
        {
            var t = callInfo.Arg<Ticket>();
            t.Id = ++callCount;
            return t;
        });

        // Act
        var result = (await _sut.HandleAsync(new CreateTicketsCommand(10, 3))).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        await _ticketRepo.Received(3).AddAsync(Arg.Any<Ticket>());
        Assert.All(result, dto =>
        {
            Assert.Equal(10, dto.EventId);
            Assert.Equal("Available", dto.Status);
        });
    }
}
