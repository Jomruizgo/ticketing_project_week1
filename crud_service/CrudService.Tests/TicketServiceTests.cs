using CrudService.Models.Entities;
using CrudService.Repositories;
using CrudService.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace CrudService.Tests;

public class TicketServiceTests
{
    private readonly ITicketRepository _ticketRepo;
    private readonly ITicketHistoryRepository _historyRepo;
    private readonly ILogger<TicketService> _logger;
    private readonly TicketService _sut;

    public TicketServiceTests()
    {
        _ticketRepo = Substitute.For<ITicketRepository>();
        _historyRepo = Substitute.For<ITicketHistoryRepository>();
        _logger = Substitute.For<ILogger<TicketService>>();
        _sut = new TicketService(_ticketRepo, _historyRepo, _logger);
    }

    [Fact]
    public async Task GetTicketsByEventAsync_ReturnsMappedDtos()
    {
        // Arrange
        var tickets = new List<Ticket>
        {
            new() { Id = 1, EventId = 10, Status = TicketStatus.Available, Version = 0 },
            new() { Id = 2, EventId = 10, Status = TicketStatus.Reserved, Version = 1 }
        };
        _ticketRepo.GetByEventIdAsync(10).Returns(tickets);

        // Act
        var result = (await _sut.GetTicketsByEventAsync(10)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Available", result[0].Status);
        Assert.Equal("Reserved", result[1].Status);
        Assert.Equal(10, result[0].EventId);
    }

    [Fact]
    public async Task GetTicketByIdAsync_Found_ReturnsDto()
    {
        // Arrange
        var ticket = new Ticket
        {
            Id = 1,
            EventId = 10,
            Status = TicketStatus.Reserved,
            ReservedBy = "user@test.com",
            OrderId = "ORD-123",
            Version = 2
        };
        _ticketRepo.GetByIdAsync(1).Returns(ticket);

        // Act
        var result = await _sut.GetTicketByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Reserved", result.Status);
        Assert.Equal("user@test.com", result.ReservedBy);
        Assert.Equal("ORD-123", result.OrderId);
        Assert.Equal(2, result.Version);
    }

    [Fact]
    public async Task GetTicketByIdAsync_NotFound_ReturnsNull()
    {
        // Arrange
        _ticketRepo.GetByIdAsync(999).Returns((Ticket?)null);

        // Act
        var result = await _sut.GetTicketByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateTicketsAsync_CreatesRequestedQuantity()
    {
        // Arrange
        var callCount = 0;
        _ticketRepo.AddAsync(Arg.Any<Ticket>()).Returns(callInfo =>
        {
            var t = callInfo.Arg<Ticket>();
            t.Id = ++callCount;
            return t;
        });

        // Act
        var result = (await _sut.CreateTicketsAsync(10, 3)).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        await _ticketRepo.Received(3).AddAsync(Arg.Any<Ticket>());
        Assert.All(result, dto =>
        {
            Assert.Equal(10, dto.EventId);
            Assert.Equal("Available", dto.Status);
        });
    }

    [Fact]
    public async Task UpdateTicketStatusAsync_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _ticketRepo.GetByIdAsync(999).Returns((Ticket?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.UpdateTicketStatusAsync(999, "Reserved"));
    }

    [Fact]
    public async Task UpdateTicketStatusAsync_InvalidStatus_ThrowsArgumentException()
    {
        // Arrange
        var ticket = new Ticket { Id = 1, EventId = 10, Status = TicketStatus.Available };
        _ticketRepo.GetByIdAsync(1).Returns(ticket);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.UpdateTicketStatusAsync(1, "InvalidStatus"));
    }

    [Fact]
    public async Task UpdateTicketStatusAsync_ValidTransition_UpdatesAndRecordsHistory()
    {
        // Arrange
        var ticket = new Ticket { Id = 1, EventId = 10, Status = TicketStatus.Available, Version = 0 };
        _ticketRepo.GetByIdAsync(1).Returns(ticket);
        _ticketRepo.UpdateAsync(Arg.Any<Ticket>()).Returns(callInfo => callInfo.Arg<Ticket>());

        // Act
        var result = await _sut.UpdateTicketStatusAsync(1, "Reserved", "Usuario reservó");

        // Assert
        Assert.Equal("Reserved", result.Status);
        Assert.Equal(1, result.Version);
        await _historyRepo.Received(1).AddAsync(Arg.Is<TicketHistory>(h =>
            h.TicketId == 1 &&
            h.OldStatus == TicketStatus.Available &&
            h.NewStatus == TicketStatus.Reserved &&
            h.Reason == "Usuario reservó"));
    }

    [Fact]
    public async Task ReleaseTicketAsync_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _ticketRepo.GetByIdAsync(999).Returns((Ticket?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.ReleaseTicketAsync(999));
    }

    [Fact]
    public async Task ReleaseTicketAsync_ClearsReservationFields()
    {
        // Arrange
        var ticket = new Ticket
        {
            Id = 1,
            EventId = 10,
            Status = TicketStatus.Reserved,
            ReservedAt = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            ReservedBy = "user@test.com",
            OrderId = "ORD-123",
            Version = 1
        };
        _ticketRepo.GetByIdAsync(1).Returns(ticket);
        _ticketRepo.UpdateAsync(Arg.Any<Ticket>()).Returns(callInfo => callInfo.Arg<Ticket>());

        // Act
        var result = await _sut.ReleaseTicketAsync(1, "Expirado");

        // Assert
        Assert.Equal("Available", result.Status);
        Assert.Null(result.ReservedAt);
        Assert.Null(result.ExpiresAt);
        Assert.Null(result.ReservedBy);
        Assert.Null(result.OrderId);
        Assert.Equal(2, result.Version);
    }

    [Fact]
    public async Task ReleaseTicketAsync_NoReason_UsesDefaultReason()
    {
        // Arrange
        var ticket = new Ticket
        {
            Id = 1,
            EventId = 10,
            Status = TicketStatus.Reserved,
            Version = 0
        };
        _ticketRepo.GetByIdAsync(1).Returns(ticket);
        _ticketRepo.UpdateAsync(Arg.Any<Ticket>()).Returns(callInfo => callInfo.Arg<Ticket>());

        // Act
        await _sut.ReleaseTicketAsync(1);

        // Assert
        await _historyRepo.Received(1).AddAsync(Arg.Is<TicketHistory>(h =>
            h.Reason == "Ticket liberado"));
    }

    [Fact]
    public async Task GetExpiredTicketsAsync_ReturnsMappedDtos()
    {
        // Arrange
        var expiredTickets = new List<Ticket>
        {
            new() { Id = 1, EventId = 10, Status = TicketStatus.Reserved, ExpiresAt = DateTime.UtcNow.AddMinutes(-5), Version = 1 },
            new() { Id = 2, EventId = 10, Status = TicketStatus.Reserved, ExpiresAt = DateTime.UtcNow.AddMinutes(-10), Version = 1 }
        };
        _ticketRepo.GetExpiredAsync(Arg.Any<DateTime>()).Returns(expiredTickets);

        // Act
        var result = (await _sut.GetExpiredTicketsAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, dto => Assert.Equal("Reserved", dto.Status));
    }
}
