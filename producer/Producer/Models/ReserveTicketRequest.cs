namespace Producer.Models;

/// <summary>
/// Request para reservar un ticket
/// </summary>
public class ReserveTicketRequest
{
    /// <summary>
    /// ID del evento
    /// </summary>
    public long EventId { get; set; }

    /// <summary>
    /// ID del ticket a reservar
    /// </summary>
    public long TicketId { get; set; }

    /// <summary>
    /// ID de la orden
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// Usuario que hace la reserva
    /// </summary>
    public string? ReservedBy { get; set; }

    /// <summary>
    /// Momento en que expira la reserva (en segundos desde ahora)
    /// </summary>
    public int ExpiresInSeconds { get; set; } = 300;
}
