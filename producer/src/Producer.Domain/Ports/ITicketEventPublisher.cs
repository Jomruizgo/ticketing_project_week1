using Producer.Domain.Events;

namespace Producer.Domain.Ports;

public interface ITicketEventPublisher
{
    Task PublishAsync(TicketReservedEvent evt, CancellationToken ct = default);
}
