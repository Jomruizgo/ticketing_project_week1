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
    private readonly IEventRepository _eventRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly EventService _sut;

    public EventServiceTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _ticketRepository = Substitute.For<ITicketRepository>();
        var logger = Substitute.For<ILogger<EventService>>();
        _sut = new EventService(_eventRepository, _ticketRepository, logger);
    }

    private static Event CreateEvent(long id = 1, string name = "Concert") => new()
    {
        Id = id,
        Name = name,
        StartsAt = new DateTime(2025, 6, 15, 20, 0, 0, DateTimeKind.Utc),
        Tickets = new List<Ticket>
        {
            new() { Id = 1, Status = TicketStatus.Available },
            new() { Id = 2, Status = TicketStatus.Reserved },
            new() { Id = 3, Status = TicketStatus.Paid },
            new() { Id = 4, Status = TicketStatus.Available }
        }
    };

    [Fact]
    public async Task GetAllEventsAsync_ReturnsAllMappedDtos()
    {
        var events = new List<Event> { CreateEvent(1, "Event A"), CreateEvent(2, "Event B") };
        _eventRepository.GetAllAsync().Returns(events);

        var result = (await _sut.GetAllEventsAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Event A", result[0].Name);
        Assert.Equal("Event B", result[1].Name);
    }

    [Fact]
    public async Task GetEventByIdAsync_Found_ReturnsMappedDto()
    {
        var evt = CreateEvent();
        _eventRepository.GetByIdAsync(1).Returns(evt);

        var result = await _sut.GetEventByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Concert", result!.Name);
        Assert.Equal(2, result.AvailableTickets);
        Assert.Equal(1, result.ReservedTickets);
        Assert.Equal(1, result.PaidTickets);
    }

    [Fact]
    public async Task GetEventByIdAsync_NotFound_ReturnsNull()
    {
        _eventRepository.GetByIdAsync(99).Returns((Event?)null);

        var result = await _sut.GetEventByIdAsync(99);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateEventAsync_CreatesAndReturnsDto()
    {
        var request = new CreateEventRequest
        {
            Name = "New Event",
            StartsAt = new DateTime(2025, 12, 1, 18, 0, 0, DateTimeKind.Utc)
        };
        _eventRepository.AddAsync(Arg.Any<Event>()).Returns(ci =>
        {
            var e = ci.Arg<Event>();
            e.Id = 10;
            e.Tickets = new List<Ticket>();
            return e;
        });

        var result = await _sut.CreateEventAsync(request);

        Assert.Equal(10, result.Id);
        Assert.Equal("New Event", result.Name);
        await _eventRepository.Received(1).AddAsync(Arg.Is<Event>(e => e.Name == "New Event"));
    }

    [Fact]
    public async Task UpdateEventAsync_NotFound_ThrowsKeyNotFoundException()
    {
        _eventRepository.GetByIdAsync(99).Returns((Event?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.UpdateEventAsync(99, new UpdateEventRequest { Name = "Updated" }));
    }

    [Fact]
    public async Task UpdateEventAsync_PartialUpdate_OnlyUpdatesProvidedFields()
    {
        var existing = CreateEvent();
        var originalDate = existing.StartsAt;
        _eventRepository.GetByIdAsync(1).Returns(existing);
        _eventRepository.UpdateAsync(Arg.Any<Event>()).Returns(ci => ci.Arg<Event>());

        var result = await _sut.UpdateEventAsync(1, new UpdateEventRequest { Name = "Updated Concert" });

        Assert.Equal("Updated Concert", result.Name);
        Assert.Equal(originalDate, existing.StartsAt);
    }

    [Fact]
    public async Task DeleteEventAsync_Found_ReturnsTrue()
    {
        _eventRepository.DeleteAsync(1).Returns(true);

        var result = await _sut.DeleteEventAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteEventAsync_NotFound_ReturnsFalse()
    {
        _eventRepository.DeleteAsync(99).Returns(false);

        var result = await _sut.DeleteEventAsync(99);

        Assert.False(result);
    }
}
