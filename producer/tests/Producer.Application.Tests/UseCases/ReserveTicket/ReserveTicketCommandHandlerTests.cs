using NSubstitute;
using Producer.Application.UseCases.ReserveTicket;
using Producer.Domain.Events;
using Producer.Domain.Ports;
using Xunit;

namespace Producer.Application.Tests.UseCases.ReserveTicket;

public class ReserveTicketCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldPublishTicketReservedEventWithMappedFields()
    {
        var publisher = Substitute.For<ITicketEventPublisher>();
        var handler = new ReserveTicketCommandHandler(publisher);
        var command = new ReserveTicketCommand(
            EventId: 10,
            TicketId: 20,
            OrderId: "ORD-123",
            ReservedBy: "user@email.com",
            ExpiresInSeconds: 600);

        await handler.HandleAsync(command, CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            Arg.Is<TicketReservedEvent>(evt =>
                evt.EventId == command.EventId &&
                evt.TicketId == command.TicketId &&
                evt.OrderId == command.OrderId &&
                evt.ReservedBy == command.ReservedBy &&
                evt.ReservationDurationSeconds == command.ExpiresInSeconds),
            CancellationToken.None);
    }
}
