using Xunit;
using CrudService.Application.Dtos;
using CrudService.Application.Services;
using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CrudService.Application.Tests;

/// <summary>
/// Pruebas de Caja Blanca — Capa Aplicación — TicketService.
///
/// Validan la lógica de negocio del servicio sin tocar infraestructura real.
/// Se usan sustitutos (mocks) para ITicketRepository e ITicketHistoryRepository.
///
/// 7 Principios — Principio 1 (Testing shows presence of defects, not absence).
/// 7 Principios — Principio 3 (Early testing): lógica validada en la capa más interna.
/// </summary>
public class TicketServiceTests
{
    private readonly ITicketRepository _ticketRepo;
    private readonly ITicketHistoryRepository _historyRepo;
    private readonly TicketService _sut;

    public TicketServiceTests()
    {
        _ticketRepo = Substitute.For<ITicketRepository>();
        _historyRepo = Substitute.For<ITicketHistoryRepository>();
        var logger = Substitute.For<ILogger<TicketService>>();
        _sut = new TicketService(_ticketRepo, _historyRepo, logger);
    }

    // ─── GetTicketByIdAsync ──────────────────────────────────────────────────────

    [Fact(DisplayName = "GetTicketById: ticket existente → retorna DTO con datos correctos")]
    public async Task GetTicketByIdAsync_ExistingTicket_ReturnsMappedDto()
    {
        var ticket = new Ticket { Id = 10, EventId = 2, Status = TicketStatus.Available, Version = 0 };
        _ticketRepo.GetByIdAsync(10).Returns(ticket);

        var result = await _sut.GetTicketByIdAsync(10);

        Assert.NotNull(result);
        Assert.Equal(10, result!.Id);
        Assert.Equal("Available", result.Status);
    }

    [Fact(DisplayName = "GetTicketById: ticket no existe → retorna null")]
    public async Task GetTicketByIdAsync_NotFound_ReturnsNull()
    {
        _ticketRepo.GetByIdAsync(Arg.Any<long>()).Returns((Ticket?)null);

        var result = await _sut.GetTicketByIdAsync(99);

        Assert.Null(result);
    }

    // ─── GetTicketsByEventAsync ──────────────────────────────────────────────────

    [Fact(DisplayName = "GetTicketsByEvent: evento con tickets → retorna lista completa")]
    public async Task GetTicketsByEventAsync_EventWithTickets_ReturnsAllTickets()
    {
        var tickets = new List<Ticket>
        {
            new() { Id = 1, EventId = 5, Status = TicketStatus.Available },
            new() { Id = 2, EventId = 5, Status = TicketStatus.Reserved }
        };
        _ticketRepo.GetByEventIdAsync(5).Returns(tickets);

        var result = (await _sut.GetTicketsByEventAsync(5)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Status == "Available");
        Assert.Contains(result, t => t.Status == "Reserved");
    }

    [Fact(DisplayName = "GetTicketsByEvent: evento sin tickets → retorna lista vacía")]
    public async Task GetTicketsByEventAsync_NoTickets_ReturnsEmptyList()
    {
        _ticketRepo.GetByEventIdAsync(Arg.Any<long>()).Returns(Enumerable.Empty<Ticket>());

        var result = await _sut.GetTicketsByEventAsync(999);

        Assert.Empty(result);
    }

    // ─── ReleaseTicketAsync ──────────────────────────────────────────────────────

    [Fact(DisplayName = "ReleaseTicket: ticket reservado → lo libera y resetea campos")]
    public async Task ReleaseTicketAsync_ReservedTicket_ReleasesAndClearsFields()
    {
        var ticket = new Ticket
        {
            Id = 7,
            EventId = 1,
            Status = TicketStatus.Reserved,
            ReservedBy = "user-1",
            OrderId = "order-abc",
            ReservedAt = DateTime.UtcNow,
            Version = 1
        };
        _ticketRepo.GetByIdAsync(7).Returns(ticket);
        _ticketRepo.UpdateAsync(Arg.Any<Ticket>()).Returns(ci => ci.Arg<Ticket>());
        _historyRepo.AddAsync(Arg.Any<TicketHistory>()).Returns(ci => ci.Arg<TicketHistory>());

        var result = await _sut.ReleaseTicketAsync(7, "test-release");

        Assert.Equal("Available", result.Status);
        await _historyRepo.Received(1).AddAsync(Arg.Any<TicketHistory>());
    }

    [Fact(DisplayName = "ReleaseTicket: ticket no encontrado → lanza KeyNotFoundException")]
    public async Task ReleaseTicketAsync_TicketNotFound_ThrowsKeyNotFoundException()
    {
        _ticketRepo.GetByIdAsync(Arg.Any<long>()).Returns((Ticket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.ReleaseTicketAsync(999));
    }

    // ─── UpdateTicketStatusAsync ─────────────────────────────────────────────────

    [Fact(DisplayName = "UpdateTicketStatus: estado válido → actualiza y registra historial")]
    public async Task UpdateTicketStatusAsync_ValidStatus_UpdatesAndRecordsHistory()
    {
        var ticket = new Ticket { Id = 3, EventId = 1, Status = TicketStatus.Available, Version = 0 };
        _ticketRepo.GetByIdAsync(3).Returns(ticket);
        _ticketRepo.UpdateAsync(Arg.Any<Ticket>()).Returns(ci => ci.Arg<Ticket>());
        _historyRepo.AddAsync(Arg.Any<TicketHistory>()).Returns(ci => ci.Arg<TicketHistory>());

        var result = await _sut.UpdateTicketStatusAsync(3, "Reserved");

        Assert.Equal("Reserved", result.Status);
        await _historyRepo.Received(1).AddAsync(Arg.Any<TicketHistory>());
    }

    [Fact(DisplayName = "UpdateTicketStatus: estado inválido → lanza ArgumentException")]
    public async Task UpdateTicketStatusAsync_InvalidStatus_ThrowsArgumentException()
    {
        var ticket = new Ticket { Id = 3, EventId = 1, Status = TicketStatus.Available };
        _ticketRepo.GetByIdAsync(3).Returns(ticket);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.UpdateTicketStatusAsync(3, "ESTADO_INVALIDO"));
    }

    // ─── GetExpiredTicketsAsync ──────────────────────────────────────────────────

    [Fact(DisplayName = "GetExpiredTickets → retorna solo tickets expirados")]
    public async Task GetExpiredTicketsAsync_ReturnsExpiredList()
    {
        var expired = new List<Ticket>
        {
            new() { Id = 11, EventId = 1, Status = TicketStatus.Reserved, ExpiresAt = DateTime.UtcNow.AddMinutes(-5) }
        };
        _ticketRepo.GetExpiredAsync(Arg.Any<DateTime>()).Returns(expired);

        var result = (await _sut.GetExpiredTicketsAsync()).ToList();

        Assert.Single(result);
        Assert.Equal(11, result[0].Id);
    }
}
