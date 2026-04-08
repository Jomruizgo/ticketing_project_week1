namespace CrudService.Infrastructure.Persistence.Repositories;

using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;

public class NotificationDeliveryRepository : INotificationDeliveryRepository
{
    private readonly TicketingDbContext _context;

    public NotificationDeliveryRepository(TicketingDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationDelivery> AddAsync(NotificationDelivery delivery)
    {
        _context.NotificationDeliveries.Add(delivery);
        await _context.SaveChangesAsync();
        return delivery;
    }

    public async Task UpdateAsync(NotificationDelivery delivery)
    {
        _context.NotificationDeliveries.Update(delivery);
        await _context.SaveChangesAsync();
    }
}
