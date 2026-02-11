namespace ReservationService.Worker.Models;

/// <summary>
/// DTO para el mensaje recibido desde RabbitMQ cuando se solicita una reserva.
/// Debe coincidir con el formato que envía el Producer.
/// </summary>
public class ReservationMessage
{
    public long TicketId { get; set; }
    public long EventId { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string ReservedBy { get; set; } = string.Empty;
    public int ReservationDurationSeconds { get; set; } = 300;
    public DateTime PublishedAt { get; set; }
}
