using CrudService.Application.UseCases.Events.CreateEvent;
using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CrudService.Application.Tests.Events;

public class CreateEventCommandHandlerTests
{
    private readonly IEventRepository _eventRepo;
    private readonly ILogger<CreateEventCommandHandler> _logger;
    private readonly CreateEventCommandHandler _sut;

    public CreateEventCommandHandlerTests()
    {
        _eventRepo = Substitute.For<IEventRepository>();
        _logger = Substitute.For<ILogger<CreateEventCommandHandler>>();
        _sut = new CreateEventCommandHandler(_eventRepo, _logger);
    }

    [Fact]
    public async Task HandleAsync_CreatesAndReturnsDto()
    {
        // Arrange
        var command = new CreateEventCommand("Festival", new DateTime(2026, 8, 1));
        _eventRepo.AddAsync(Arg.Any<Event>()).Returns(callInfo =>
        {
            var e = callInfo.Arg<Event>();
            e.Id = 10;
            e.Tickets = new List<Ticket>();
            return e;
        });

        // Act
        var result = await _sut.HandleAsync(command);

        // Assert
        Assert.Equal(10, result.Id);
        Assert.Equal("Festival", result.Name);
        Assert.Equal(new DateTime(2026, 8, 1), result.StartsAt);
        await _eventRepo.Received(1).AddAsync(Arg.Is<Event>(e =>
            e.Name == "Festival" && e.StartsAt == new DateTime(2026, 8, 1)));
    }
}
