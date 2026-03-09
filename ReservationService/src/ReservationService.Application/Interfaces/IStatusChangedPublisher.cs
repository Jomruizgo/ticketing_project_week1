namespace ReservationService.Application.Interfaces;

public interface IStatusChangedPublisher
{
    Task PublishAsync(long ticketId, string newStatus, CancellationToken cancellationToken = default);
}
