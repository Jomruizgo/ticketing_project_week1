namespace CrudService.Domain.Interfaces;

public interface ITicketReservationPort
{
    Task<bool> TryReserveForWaitlistAsync(long ticketId, string buyerEmail);
}
