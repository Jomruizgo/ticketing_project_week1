namespace ReservationService.Domain.Exceptions;

public class TicketNotAvailableException : Exception
{
    public long TicketId { get; }

    public TicketNotAvailableException(long ticketId)
        : base($"Ticket {ticketId} is not available for reservation")
    {
        TicketId = ticketId;
    }
}
