using CrudService.Application.Dtos;
using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;

namespace CrudService.Application.UseCases.Tickets.GetTicketsByEvent;

public class GetTicketsByEventQueryHandler
{
    private readonly ITicketRepository _ticketRepository;

    public GetTicketsByEventQueryHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<IEnumerable<TicketDto>> HandleAsync(GetTicketsByEventQuery query)
    {
        var tickets = await _ticketRepository.GetByEventIdAsync(query.EventId);
        return tickets.Select(MapToDto);
    }

    internal static TicketDto MapToDto(Ticket ticket) => new()
    {
        Id = ticket.Id,
        EventId = ticket.EventId,
        Status = ticket.Status.ToString(),
        ReservedAt = ticket.ReservedAt,
        ExpiresAt = ticket.ExpiresAt,
        PaidAt = ticket.PaidAt,
        OrderId = ticket.OrderId,
        ReservedBy = ticket.ReservedBy,
        Version = ticket.Version
    };
}
