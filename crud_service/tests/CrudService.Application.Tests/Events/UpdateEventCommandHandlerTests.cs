using CrudService.Application.UseCases.Events.UpdateEvent;
using CrudService.Domain.Entities;
using CrudService.Domain.Exceptions;
using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CrudService.Application.Tests.Events;

public class UpdateEventCommandHandlerTests
{
    private readonly IEventRepository _eventRepo;
    private readonly ILogger<UpdateEventCommandHandler> _logger;
    private readonly UpdateEventCommandHandler _sut;

    public UpdateEventCommandHandlerTests()
    {
        _eventRepo = Substitute.For<IEventRepository>();
        _logger = Substitute.For<ILogger<UpdateEventCommandHandler>>();
        _sut = new UpdateEventCommandHandler(_eventRepo, _logger);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ThrowsEventNotFoundException()
    {
        // Arrange
        _eventRepo.GetByIdAsync(999).Returns((Event?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EventNotFoundException>(
            () => _sut.HandleAsync(new UpdateEventCommand(999, "Nuevo Nombre", null)));
    }

    [Fact]
    public async Task HandleAsync_PartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var originalDate = new DateTime(2026, 6, 15);
        var @event = new Event { Id = 1, Name = "Original", StartsAt = originalDate, Tickets = new List<Ticket>() };
        _eventRepo.GetByIdAsync(1).Returns(@event);
        _eventRepo.UpdateAsync(Arg.Any<Event>()).Returns(callInfo => callInfo.Arg<Event>());

        // Act
        var result = await _sut.HandleAsync(new UpdateEventCommand(1, "Actualizado", null));

        // Assert
        Assert.Equal("Actualizado", result.Name);
        Assert.Equal(originalDate, result.StartsAt);
    }
}
