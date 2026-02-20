using Producer.Domain.Events;
using Producer.Domain.Ports;

namespace Producer.Application.UseCases.RequestPayment;

public class RequestPaymentCommandHandler
{
    private readonly IPaymentEventPublisher _paymentEventPublisher;

    public RequestPaymentCommandHandler(IPaymentEventPublisher paymentEventPublisher)
    {
        _paymentEventPublisher = paymentEventPublisher;
    }

    public async Task<RequestPaymentResponse> HandleAsync(RequestPaymentCommand command, CancellationToken ct = default)
    {
        var evt = new PaymentRequestedEvent
        {
            TicketId = command.TicketId,
            EventId = command.EventId,
            AmountCents = command.AmountCents,
            Currency = command.Currency,
            PaymentBy = command.PaymentBy,
            PaymentMethodId = command.PaymentMethodId,
            TransactionRef = command.TransactionRef ?? $"TXN-{Guid.NewGuid()}",
            RequestedAt = DateTime.UtcNow
        };

        await _paymentEventPublisher.PublishAsync(evt, ct);

        return new RequestPaymentResponse(command.TicketId, "Solicitud de pago encolada para procesamiento");
    }
}
