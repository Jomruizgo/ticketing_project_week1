namespace CrudService.Domain.Interfaces;

using CrudService.Domain.Entities;

public interface INotificationDeliveryRepository
{
    Task<NotificationDelivery> AddAsync(NotificationDelivery delivery);
    Task UpdateAsync(NotificationDelivery delivery);
}
