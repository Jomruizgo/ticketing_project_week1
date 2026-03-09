using CrudService.Application.UseCases.Events.GetAllEvents;
using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace CrudService.Application.Tests.Events;

public class GetAllEventsQueryHandlerTests
{
    private readonly IEventRepository _eventRepo;
    private readonly GetAllEventsQueryHandler _sut;

    public GetAllEventsQueryHandlerTests()
    {
        _eventRepo = Substitute.For<IEventRepository>();
        _sut = new GetAllEventsQueryHandler(_eventRepo);
    }

    [Fact]
    public async Task HandleAsync_ReturnsAllMappedDtos()
    {
        // Arrange
        var events = new List<Event>
        {
            new() { Id = 1, Name = "Concierto", StartsAt = new DateTime(2026, 6, 15), Tickets = new List<Ticket>() },
            new() { Id = 2, Name = "Teatro",    StartsAt = new DateTime(2026, 7, 20), Tickets = new List<Ticket>() }
        };
        _eventRepo.GetAllAsync().Returns(events);

        // Act
        var result = (await _sut.HandleAsync(new GetAllEventsQuery())).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Concierto", result[0].Name);
        Assert.Equal("Teatro", result[1].Name);
    }
}
