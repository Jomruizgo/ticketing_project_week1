using Producer.Domain.Events;
using Producer.Domain.Ports;

namespace Producer.Application.UseCases.ReserveTicket;

public class ReserveTicketCommandHandler
{
    private readonly ITicketEventPublisher _ticketEventPublisher;

    public ReserveTicketCommandHandler(ITicketEventPublisher ticketEventPublisher)
    {
        _ticketEventPublisher = ticketEventPublisher;
    }

    public async Task<ReserveTicketResponse> HandleAsync(ReserveTicketCommand command, CancellationToken ct = default)
    {
        var evt = new TicketReservedEvent
        {
            TicketId = command.TicketId,
            EventId = command.EventId,
            OrderId = command.OrderId,
            ReservedBy = command.ReservedBy,
            ReservationDurationSeconds = command.ExpiresInSeconds,
            PublishedAt = DateTime.UtcNow
        };

        await _ticketEventPublisher.PublishAsync(evt, ct);

        return new ReserveTicketResponse(command.TicketId, "Reserva procesada");
    }
}
