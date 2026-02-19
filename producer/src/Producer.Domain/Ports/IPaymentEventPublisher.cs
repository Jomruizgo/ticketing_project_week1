using Producer.Domain.Events;

namespace Producer.Domain.Ports;

public interface IPaymentEventPublisher
{
    Task PublishAsync(PaymentRequestedEvent evt, CancellationToken ct = default);
}
