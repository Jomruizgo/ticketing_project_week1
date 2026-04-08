namespace CrudService.Domain.Interfaces;

public interface IInventoryReturnPort
{
    Task ReturnToInventoryAsync(long ticketId, long eventId);
}
