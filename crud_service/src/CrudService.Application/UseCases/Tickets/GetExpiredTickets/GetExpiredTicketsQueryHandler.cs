using CrudService.Application.Dtos;
using CrudService.Application.UseCases.Tickets.GetTicketsByEvent;
using CrudService.Domain.Interfaces;

namespace CrudService.Application.UseCases.Tickets.GetExpiredTickets;

public class GetExpiredTicketsQueryHandler
{
    private readonly ITicketRepository _ticketRepository;

    public GetExpiredTicketsQueryHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<IEnumerable<TicketDto>> HandleAsync(GetExpiredTicketsQuery query)
    {
        var tickets = await _ticketRepository.GetExpiredAsync(DateTime.UtcNow);
        return tickets.Select(GetTicketsByEventQueryHandler.MapToDto);
    }
}
