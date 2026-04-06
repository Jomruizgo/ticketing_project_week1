using NSubstitute;
using Producer.Application.UseCases.RequestPayment;
using Producer.Domain.Events;
using Producer.Domain.Ports;
using Xunit;

namespace Producer.Application.Tests.UseCases.RequestPayment;

public class RequestPaymentCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldPublishPaymentRequestedEventWithMappedFields()
    {
        var publisher = Substitute.For<IPaymentEventPublisher>();
        var handler = new RequestPaymentCommandHandler(publisher);
        var command = new RequestPaymentCommand(
            TicketId: 11,
            EventId: 22,
            AmountCents: 5000,
            Currency: "USD",
            PaymentBy: "user@email.com",
            PaymentMethodId: "card_123",
            TransactionRef: "TXN-ABC");

        await handler.HandleAsync(command, CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            Arg.Is<PaymentRequestedEvent>(evt =>
                evt.TicketId == command.TicketId &&
                evt.EventId == command.EventId &&
                evt.AmountCents == command.AmountCents &&
                evt.Currency == command.Currency &&
                evt.PaymentBy == command.PaymentBy &&
                evt.PaymentMethodId == command.PaymentMethodId &&
                evt.TransactionRef == command.TransactionRef),
            CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_ShouldGenerateTransactionRef_WhenCommandTransactionRefIsNull()
    {
        var publisher = Substitute.For<IPaymentEventPublisher>();
        var handler = new RequestPaymentCommandHandler(publisher);
        var command = new RequestPaymentCommand(
            TicketId: 11,
            EventId: 22,
            AmountCents: 5000,
            Currency: "USD",
            PaymentBy: "user@email.com",
            PaymentMethodId: "card_123",
            TransactionRef: null);

        await handler.HandleAsync(command, CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            Arg.Is<PaymentRequestedEvent>(evt =>
                !string.IsNullOrWhiteSpace(evt.TransactionRef) &&
                evt.TransactionRef.StartsWith("TXN-")),
            CancellationToken.None);
    }
}
