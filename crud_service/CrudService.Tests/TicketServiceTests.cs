using CrudService.Models.Entities;
using CrudService.Repositories;
using CrudService.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CrudService.Tests;

public class TicketServiceTests
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketHistoryRepository _historyRepository;
    private readonly TicketService _sut;

    public TicketServiceTests()
    {
        _ticketRepository = Substitute.For<ITicketRepository>();
        _historyRepository = Substitute.For<ITicketHistoryRepository>();
        var logger = Substitute.For<ILogger<TicketService>>();
        _sut = new TicketService(_ticketRepository, _historyRepository, logger);
    }

    private static Ticket CreateTicket(long id = 1, TicketStatus status = TicketStatus.Available) => new()
    {
        Id = id,
        EventId = 100,
        Status = status,
        Version = 1,
        Payments = new List<Payment>(),
        History = new List<TicketHistory>()
    };

    // === GetTicketsByEventAsync ===

    [Fact]
    public async Task GetTicketsByEventAsync_ReturnsMappedDtos()
    {
        var tickets = new List<Ticket>
        {
            CreateTicket(1, TicketStatus.Available),
            CreateTicket(2, TicketStatus.Reserved)
        };
        _ticketRepository.GetByEventIdAsync(100).Returns(tickets);

        var result = (await _sut.GetTicketsByEventAsync(100)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Available", result[0].Status);
        Assert.Equal("Reserved", result[1].Status);
    }

    // === GetTicketByIdAsync ===

    [Fact]
    public async Task GetTicketByIdAsync_Found_ReturnsDto()
    {
        _ticketRepository.GetByIdAsync(1).Returns(CreateTicket());

        var result = await _sut.GetTicketByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
    }

    [Fact]
    public async Task GetTicketByIdAsync_NotFound_ReturnsNull()
    {
        _ticketRepository.GetByIdAsync(99).Returns((Ticket?)null);

        var result = await _sut.GetTicketByIdAsync(99);

        Assert.Null(result);
    }

    // === CreateTicketsAsync ===

    [Fact]
    public async Task CreateTicketsAsync_CreatesRequestedQuantity()
    {
        _ticketRepository.AddAsync(Arg.Any<Ticket>()).Returns(ci =>
        {
            var t = ci.Arg<Ticket>();
            t.Id = 10;
            t.Payments = new List<Payment>();
            t.History = new List<TicketHistory>();
            return t;
        });

        var result = (await _sut.CreateTicketsAsync(100, 3)).ToList();

        Assert.Equal(3, result.Count);
        await _ticketRepository.Received(3).AddAsync(Arg.Is<Ticket>(t =>
            t.EventId == 100 && t.Status == TicketStatus.Available));
    }

    // === UpdateTicketStatusAsync ===

    [Fact]
    public async Task UpdateTicketStatusAsync_NotFound_ThrowsKeyNotFoundException()
    {
        _ticketRepository.GetByIdAsync(99).Returns((Ticket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.UpdateTicketStatusAsync(99, "Reserved"));
    }

    [Fact]
    public async Task UpdateTicketStatusAsync_InvalidStatus_ThrowsArgumentException()
    {
        _ticketRepository.GetByIdAsync(1).Returns(CreateTicket());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.UpdateTicketStatusAsync(1, "InvalidStatus"));
    }

    [Fact]
    public async Task UpdateTicketStatusAsync_ValidTransition_UpdatesAndRecordsHistory()
    {
        var ticket = CreateTicket(1, TicketStatus.Available);
        _ticketRepository.GetByIdAsync(1).Returns(ticket);
        _ticketRepository.UpdateAsync(Arg.Any<Ticket>()).Returns(ci => ci.Arg<Ticket>());
        _historyRepository.AddAsync(Arg.Any<TicketHistory>()).Returns(ci => ci.Arg<TicketHistory>());

        var result = await _sut.UpdateTicketStatusAsync(1, "Reserved", "User reservation");

        Assert.Equal("Reserved", result.Status);
        Assert.Equal(2, ticket.Version);
        await _historyRepository.Received(1).AddAsync(Arg.Is<TicketHistory>(h =>
            h.OldStatus == TicketStatus.Available &&
            h.NewStatus == TicketStatus.Reserved &&
            h.Reason == "User reservation"));
    }

    // === ReleaseTicketAsync ===

    [Fact]
    public async Task ReleaseTicketAsync_NotFound_ThrowsKeyNotFoundException()
    {
        _ticketRepository.GetByIdAsync(99).Returns((Ticket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.ReleaseTicketAsync(99));
    }

    [Fact]
    public async Task ReleaseTicketAsync_ClearsReservationFields()
    {
        var ticket = CreateTicket(1, TicketStatus.Reserved);
        ticket.ReservedAt = DateTime.UtcNow.AddMinutes(-5);
        ticket.ExpiresAt = DateTime.UtcNow.AddMinutes(5);
        ticket.ReservedBy = "user@test.com";
        ticket.OrderId = "order-123";

        _ticketRepository.GetByIdAsync(1).Returns(ticket);
        _ticketRepository.UpdateAsync(Arg.Any<Ticket>()).Returns(ci => ci.Arg<Ticket>());
        _historyRepository.AddAsync(Arg.Any<TicketHistory>()).Returns(ci => ci.Arg<TicketHistory>());

        var result = await _sut.ReleaseTicketAsync(1, "TTL expired");

        Assert.Equal("Available", result.Status);
        Assert.Null(ticket.ReservedAt);
        Assert.Null(ticket.ExpiresAt);
        Assert.Null(ticket.ReservedBy);
        Assert.Null(ticket.OrderId);
        Assert.Equal(2, ticket.Version);
        await _historyRepository.Received(1).AddAsync(Arg.Is<TicketHistory>(h =>
            h.OldStatus == TicketStatus.Reserved &&
            h.NewStatus == TicketStatus.Available &&
            h.Reason == "TTL expired"));
    }

    [Fact]
    public async Task ReleaseTicketAsync_NoReason_UsesDefaultReason()
    {
        var ticket = CreateTicket(1, TicketStatus.Reserved);
        _ticketRepository.GetByIdAsync(1).Returns(ticket);
        _ticketRepository.UpdateAsync(Arg.Any<Ticket>()).Returns(ci => ci.Arg<Ticket>());
        _historyRepository.AddAsync(Arg.Any<TicketHistory>()).Returns(ci => ci.Arg<TicketHistory>());

        await _sut.ReleaseTicketAsync(1);

        await _historyRepository.Received(1).AddAsync(Arg.Is<TicketHistory>(h =>
            h.Reason == "Ticket liberado"));
    }

    // === GetExpiredTicketsAsync ===

    [Fact]
    public async Task GetExpiredTicketsAsync_ReturnsMappedDtos()
    {
        var expired = new List<Ticket>
        {
            CreateTicket(1, TicketStatus.Reserved),
            CreateTicket(2, TicketStatus.Reserved)
        };
        _ticketRepository.GetExpiredAsync(Arg.Any<DateTime>()).Returns(expired);

        var result = (await _sut.GetExpiredTicketsAsync()).ToList();

        Assert.Equal(2, result.Count);
    }
}
