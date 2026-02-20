using CrudService.Application.UseCases.Tickets.GetTicketsByEvent;
using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace CrudService.Application.Tests.Tickets;

public class GetTicketsByEventQueryHandlerTests
{
    private readonly ITicketRepository _ticketRepo;
    private readonly GetTicketsByEventQueryHandler _sut;

    public GetTicketsByEventQueryHandlerTests()
    {
        _ticketRepo = Substitute.For<ITicketRepository>();
        _sut = new GetTicketsByEventQueryHandler(_ticketRepo);
    }

    [Fact]
    public async Task HandleAsync_ReturnsMappedDtos()
    {
        // Arrange
        var tickets = new List<Ticket>
        {
            new() { Id = 1, EventId = 10, Status = TicketStatus.Available, Version = 0 },
            new() { Id = 2, EventId = 10, Status = TicketStatus.Reserved,  Version = 1 }
        };
        _ticketRepo.GetByEventIdAsync(10).Returns(tickets);

        // Act
        var result = (await _sut.HandleAsync(new GetTicketsByEventQuery(10))).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Available", result[0].Status);
        Assert.Equal("Reserved", result[1].Status);
        Assert.Equal(10, result[0].EventId);
    }
}
