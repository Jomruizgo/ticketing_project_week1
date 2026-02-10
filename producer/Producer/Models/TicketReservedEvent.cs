namespace Producer.Models;

/// <summary>
/// Evento de ticket reservado que se publica a RabbitMQ
/// </summary>
public class TicketReservedEvent
{
    /// <summary>
    /// ID único del ticket
    /// </summary>
    public long TicketId { get; set; }

    /// <summary>
    /// ID del evento
    /// </summary>
    public long EventId { get; set; }

    /// <summary>
    /// ID de la orden asociada
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// Usuario que realizó la reserva
    /// </summary>
    public string? ReservedBy { get; set; }

    /// <summary>
    /// Duración de la reserva en segundos
    /// El consumer calculará ExpiresAt = Ahora + ReservationDurationSeconds
    /// </summary>
    public int ReservationDurationSeconds { get; set; } = 300;

    /// <summary>
    /// Timestamp del evento (cuando se publica desde el Producer)
    /// </summary>
    public DateTime PublishedAt { get; set; }
}
