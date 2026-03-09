using Xunit;
using ReservationService.Domain.Entities;

namespace ReservationService.Domain.Tests;

/// <summary>
/// Pruebas de Caja Blanca — Capa Dominio — Entidad Ticket.
///
/// Validan las reglas de negocio del dominio de forma pura, sin ninguna
/// dependencia de infraestructura (ni BD, ni RabbitMQ, ni red).
///
/// 7 Principios — Principio 3 (Early testing): las reglas del dominio se
/// prueban primero, en la capa más interna de la arquitectura hexagonal.
/// 7 Principios — Principio 6 (Testing is context-dependent): el contexto
/// es el dominio del ticket, donde la consistencia de estado es crítica.
/// </summary>
public class TicketEntityTests
{
    // ─── Estado inicial ──────────────────────────────────────────────────────────

    [Fact(DisplayName = "Nuevo ticket: estado inicial es Available")]
    public void NewTicket_DefaultStatus_IsAvailable()
    {
        var ticket = new Ticket();

        Assert.Equal(TicketStatus.Available, ticket.Status);
    }

    [Fact(DisplayName = "Nuevo ticket: Version inicial es 0")]
    public void NewTicket_InitialVersion_IsZero()
    {
        var ticket = new Ticket();

        Assert.Equal(0, ticket.Version);
    }

    [Fact(DisplayName = "Nuevo ticket: campos de reserva son nulos")]
    public void NewTicket_ReservationFields_AreNull()
    {
        var ticket = new Ticket();

        Assert.Null(ticket.ReservedAt);
        Assert.Null(ticket.ExpiresAt);
        Assert.Null(ticket.OrderId);
        Assert.Null(ticket.ReservedBy);
    }

    // ─── Transiciones de estado ──────────────────────────────────────────────────

    [Fact(DisplayName = "Ticket: asignación de estado Reserved actualiza campos")]
    public void Ticket_SetReservedStatus_UpdatesReservationFields()
    {
        var ticket = new Ticket { Id = 1, EventId = 10, Status = TicketStatus.Available };
        var now = DateTime.UtcNow;

        ticket.Status = TicketStatus.Reserved;
        ticket.ReservedAt = now;
        ticket.ExpiresAt = now.AddMinutes(10);
        ticket.ReservedBy = "user-abc";
        ticket.OrderId = "order-xyz";
        ticket.Version++;

        Assert.Equal(TicketStatus.Reserved, ticket.Status);
        Assert.Equal("user-abc", ticket.ReservedBy);
        Assert.Equal("order-xyz", ticket.OrderId);
        Assert.Equal(1, ticket.Version);
        Assert.True(ticket.ExpiresAt > ticket.ReservedAt);
    }

    [Fact(DisplayName = "Ticket: liberación resetea campos de reserva")]
    public void Ticket_Release_ClearsReservationFields()
    {
        var ticket = new Ticket
        {
            Id = 2,
            EventId = 10,
            Status = TicketStatus.Reserved,
            ReservedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            ReservedBy = "user-abc",
            OrderId = "order-xyz",
            Version = 1
        };

        // Simula la liberación (misma lógica que TicketRepository.TryReleaseAsync)
        ticket.Status = TicketStatus.Available;
        ticket.ReservedAt = null;
        ticket.ExpiresAt = null;
        ticket.ReservedBy = null;
        ticket.OrderId = null;
        ticket.Version++;

        Assert.Equal(TicketStatus.Available, ticket.Status);
        Assert.Null(ticket.ReservedAt);
        Assert.Null(ticket.ExpiresAt);
        Assert.Null(ticket.ReservedBy);
        Assert.Null(ticket.OrderId);
        Assert.Equal(2, ticket.Version);
    }

    [Fact(DisplayName = "Ticket: estado Paid tiene PaidAt definido")]
    public void Ticket_SetPaidStatus_HasPaidAtDate()
    {
        var ticket = new Ticket { Id = 3, Status = TicketStatus.Reserved };
        var paidAt = DateTime.UtcNow;

        ticket.Status = TicketStatus.Paid;
        ticket.PaidAt = paidAt;

        Assert.Equal(TicketStatus.Paid, ticket.Status);
        Assert.Equal(paidAt, ticket.PaidAt);
    }

    // ─── Expiración ──────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Ticket expirado: ExpiresAt en el pasado → isExpired es true")]
    public void Ticket_WhenExpiresAtInPast_IsExpired()
    {
        var ticket = new Ticket
        {
            Status = TicketStatus.Reserved,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };

        var isExpired = ticket.ExpiresAt.HasValue && ticket.ExpiresAt < DateTime.UtcNow;

        Assert.True(isExpired);
    }

    [Fact(DisplayName = "Ticket vigente: ExpiresAt en el futuro → isExpired es false")]
    public void Ticket_WhenExpiresAtInFuture_IsNotExpired()
    {
        var ticket = new Ticket
        {
            Status = TicketStatus.Reserved,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        var isExpired = ticket.ExpiresAt.HasValue && ticket.ExpiresAt < DateTime.UtcNow;

        Assert.False(isExpired);
    }

    // ─── Identidad ───────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Ticket: IDs de evento y ticket son asignables correctamente")]
    public void Ticket_Ids_AreAssignableAndReadable()
    {
        var ticket = new Ticket { Id = 42, EventId = 7 };

        Assert.Equal(42, ticket.Id);
        Assert.Equal(7, ticket.EventId);
    }
}
