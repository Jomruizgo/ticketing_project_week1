using CrudService.Models.DTOs;
using CrudService.Models.Entities;
using CrudService.Repositories;
using CrudService.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CrudService.Tests;

public class EventServiceTests
{
    private readonly IEventRepository _eventRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly ILogger<EventService> _logger;
    private readonly EventService _sut;

    public EventServiceTests()
    {
        _eventRepo = Substitute.For<IEventRepository>();
        _ticketRepo = Substitute.For<ITicketRepository>();
        _logger = Substitute.For<ILogger<EventService>>();
        _sut = new EventService(_eventRepo, _ticketRepo, _logger);
    }

    [Fact]
    public async Task GetAllEventsAsync_ReturnsAllMappedDtos()
    {
        // Arrange
        var events = new List<Event>
        {
            new() { Id = 1, Name = "Concierto", StartsAt = new DateTime(2026, 6, 15), Tickets = new List<Ticket>() },
            new() { Id = 2, Name = "Teatro", StartsAt = new DateTime(2026, 7, 20), Tickets = new List<Ticket>() }
        };
        _eventRepo.GetAllAsync().Returns(events);

        // Act
        var result = (await _sut.GetAllEventsAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Concierto", result[0].Name);
        Assert.Equal("Teatro", result[1].Name);
    }

    [Fact]
    public async Task GetEventByIdAsync_Found_ReturnsMappedDto()
    {
        // Arrange
        var @event = new Event
        {
            Id = 1,
            Name = "Concierto",
            StartsAt = new DateTime(2026, 6, 15),
            Tickets = new List<Ticket>
            {
                new() { Id = 1, Status = TicketStatus.Available },
                new() { Id = 2, Status = TicketStatus.Available },
                new() { Id = 3, Status = TicketStatus.Reserved },
                new() { Id = 4, Status = TicketStatus.Paid }
            }
        };
        _eventRepo.GetByIdAsync(1).Returns(@event);

        // Act
        var result = await _sut.GetEventByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Concierto", result.Name);
        Assert.Equal(2, result.AvailableTickets);
        Assert.Equal(1, result.ReservedTickets);
        Assert.Equal(1, result.PaidTickets);
    }

    [Fact]
    public async Task GetEventByIdAsync_NotFound_ReturnsNull()
    {
        // Arrange
        _eventRepo.GetByIdAsync(999).Returns((Event?)null);

        // Act
        var result = await _sut.GetEventByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateEventAsync_CreatesAndReturnsDto()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Name = "Festival",
            StartsAt = new DateTime(2026, 8, 1)
        };
        _eventRepo.AddAsync(Arg.Any<Event>()).Returns(callInfo =>
        {
            var e = callInfo.Arg<Event>();
            e.Id = 10;
            e.Tickets = new List<Ticket>();
            return e;
        });

        // Act
        var result = await _sut.CreateEventAsync(request);

        // Assert
        Assert.Equal(10, result.Id);
        Assert.Equal("Festival", result.Name);
        Assert.Equal(new DateTime(2026, 8, 1), result.StartsAt);
        await _eventRepo.Received(1).AddAsync(Arg.Is<Event>(e =>
            e.Name == "Festival" && e.StartsAt == new DateTime(2026, 8, 1)));
    }

    [Fact]
    public async Task UpdateEventAsync_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _eventRepo.GetByIdAsync(999).Returns((Event?)null);
        var request = new UpdateEventRequest { Name = "Nuevo Nombre" };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.UpdateEventAsync(999, request));
    }

    [Fact]
    public async Task UpdateEventAsync_PartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var originalDate = new DateTime(2026, 6, 15);
        var @event = new Event
        {
            Id = 1,
            Name = "Original",
            StartsAt = originalDate,
            Tickets = new List<Ticket>()
        };
        _eventRepo.GetByIdAsync(1).Returns(@event);
        _eventRepo.UpdateAsync(Arg.Any<Event>()).Returns(callInfo => callInfo.Arg<Event>());

        var request = new UpdateEventRequest { Name = "Actualizado" };

        // Act
        var result = await _sut.UpdateEventAsync(1, request);

        // Assert
        Assert.Equal("Actualizado", result.Name);
        Assert.Equal(originalDate, result.StartsAt);
    }

    [Fact]
    public async Task DeleteEventAsync_Found_ReturnsTrue()
    {
        // Arrange
        _eventRepo.DeleteAsync(1).Returns(true);

        // Act
        var result = await _sut.DeleteEventAsync(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteEventAsync_NotFound_ReturnsFalse()
    {
        // Arrange
        _eventRepo.DeleteAsync(999).Returns(false);

        // Act
        var result = await _sut.DeleteEventAsync(999);

        // Assert
        Assert.False(result);
    }
}
