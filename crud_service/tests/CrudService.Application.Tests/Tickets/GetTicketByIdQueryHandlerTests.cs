using CrudService.Application.UseCases.Tickets.GetTicketById;
using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace CrudService.Application.Tests.Tickets;

public class GetTicketByIdQueryHandlerTests
{
    private readonly ITicketRepository _ticketRepo;
    private readonly GetTicketByIdQueryHandler _sut;

    public GetTicketByIdQueryHandlerTests()
    {
        _ticketRepo = Substitute.For<ITicketRepository>();
        _sut = new GetTicketByIdQueryHandler(_ticketRepo);
    }

    [Fact]
    public async Task HandleAsync_Found_ReturnsDto()
    {
        // Arrange
        var ticket = new Ticket
        {
            Id = 1, EventId = 10, Status = TicketStatus.Reserved,
            ReservedBy = "user@test.com", OrderId = "ORD-123", Version = 2
        };
        _ticketRepo.GetByIdAsync(1).Returns(ticket);

        // Act
        var result = await _sut.HandleAsync(new GetTicketByIdQuery(1));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Reserved", result.Status);
        Assert.Equal("user@test.com", result.ReservedBy);
        Assert.Equal("ORD-123", result.OrderId);
        Assert.Equal(2, result.Version);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ReturnsNull()
    {
        _ticketRepo.GetByIdAsync(999).Returns((Ticket?)null);
        var result = await _sut.HandleAsync(new GetTicketByIdQuery(999));
        Assert.Null(result);
    }
}
