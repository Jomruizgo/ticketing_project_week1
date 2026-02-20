using CrudService.Application.Dtos;
using CrudService.Application.UseCases.Tickets.GetTicketsByEvent;
using CrudService.Domain.Interfaces;

namespace CrudService.Application.UseCases.Tickets.GetTicketById;

public class GetTicketByIdQueryHandler
{
    private readonly ITicketRepository _ticketRepository;

    public GetTicketByIdQueryHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<TicketDto?> HandleAsync(GetTicketByIdQuery query)
    {
        var ticket = await _ticketRepository.GetByIdAsync(query.Id);
        return ticket == null ? null : GetTicketsByEventQueryHandler.MapToDto(ticket);
    }
}
