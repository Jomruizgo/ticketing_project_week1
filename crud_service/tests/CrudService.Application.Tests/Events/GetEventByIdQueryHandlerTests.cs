using CrudService.Application.UseCases.Events.GetEventById;
using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace CrudService.Application.Tests.Events;

public class GetEventByIdQueryHandlerTests
{
    private readonly IEventRepository _eventRepo;
    private readonly GetEventByIdQueryHandler _sut;

    public GetEventByIdQueryHandlerTests()
    {
        _eventRepo = Substitute.For<IEventRepository>();
        _sut = new GetEventByIdQueryHandler(_eventRepo);
    }

    [Fact]
    public async Task HandleAsync_Found_ReturnsMappedDtoWithTicketCounts()
    {
        // Arrange
        var @event = new Event
        {
            Id = 1,
            Name = "Concierto",
            StartsAt = new DateTime(2026, 6, 15),
            Tickets = new List<Ticket>
            {
                new() { Status = TicketStatus.Available },
                new() { Status = TicketStatus.Available },
                new() { Status = TicketStatus.Reserved },
                new() { Status = TicketStatus.Paid }
            }
        };
        _eventRepo.GetByIdAsync(1).Returns(@event);

        // Act
        var result = await _sut.HandleAsync(new GetEventByIdQuery(1));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Concierto", result.Name);
        Assert.Equal(2, result.AvailableTickets);
        Assert.Equal(1, result.ReservedTickets);
        Assert.Equal(1, result.PaidTickets);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ReturnsNull()
    {
        // Arrange
        _eventRepo.GetByIdAsync(999).Returns((Event?)null);

        // Act
        var result = await _sut.HandleAsync(new GetEventByIdQuery(999));

        // Assert
        Assert.Null(result);
    }
}
