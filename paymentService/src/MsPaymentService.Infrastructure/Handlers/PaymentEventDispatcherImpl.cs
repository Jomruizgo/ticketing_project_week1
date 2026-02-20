using MsPaymentService.Application.Dtos;

namespace MsPaymentService.Infrastructure.Handlers;

public class PaymentEventDispatcherImpl : IPaymentEventDispatcher
{
    private readonly IEnumerable<IPaymentEventHandler> _handlers;

    public PaymentEventDispatcherImpl(IEnumerable<IPaymentEventHandler> handlers)
    {
        _handlers = handlers;
    }

    public async Task<ValidationResult?> DispatchAsync(string queueName, string json, CancellationToken cancellationToken = default)
    {
        var handler = _handlers.FirstOrDefault(h =>
            h.QueueName.EndsWith(queueName, StringComparison.Ordinal));

        if (handler == null)
            return null;

        return await handler.HandleAsync(json, cancellationToken);
    }
}
