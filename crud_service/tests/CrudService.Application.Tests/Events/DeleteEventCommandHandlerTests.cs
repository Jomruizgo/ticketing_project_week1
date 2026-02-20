using CrudService.Application.UseCases.Events.DeleteEvent;
using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CrudService.Application.Tests.Events;

public class DeleteEventCommandHandlerTests
{
    private readonly IEventRepository _eventRepo;
    private readonly DeleteEventCommandHandler _sut;

    public DeleteEventCommandHandlerTests()
    {
        _eventRepo = Substitute.For<IEventRepository>();
        var logger = Substitute.For<ILogger<DeleteEventCommandHandler>>();
        _sut = new DeleteEventCommandHandler(_eventRepo, logger);
    }

    [Fact]
    public async Task HandleAsync_Found_ReturnsTrue()
    {
        _eventRepo.DeleteAsync(1).Returns(true);
        var result = await _sut.HandleAsync(new DeleteEventCommand(1));
        Assert.True(result);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ReturnsFalse()
    {
        _eventRepo.DeleteAsync(999).Returns(false);
        var result = await _sut.HandleAsync(new DeleteEventCommand(999));
        Assert.False(result);
    }
}
