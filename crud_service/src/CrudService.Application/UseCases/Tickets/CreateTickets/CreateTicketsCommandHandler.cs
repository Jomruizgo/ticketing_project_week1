using CrudService.Application.Dtos;
using CrudService.Application.UseCases.Tickets.GetTicketsByEvent;
using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CrudService.Application.UseCases.Tickets.CreateTickets;

public class CreateTicketsCommandHandler
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<CreateTicketsCommandHandler> _logger;

    public CreateTicketsCommandHandler(ITicketRepository ticketRepository, ILogger<CreateTicketsCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<TicketDto>> HandleAsync(CreateTicketsCommand command)
    {
        var tickets = new List<Ticket>();

        for (int i = 0; i < command.Quantity; i++)
        {
            var ticket = new Ticket
            {
                EventId = command.EventId,
                Status = TicketStatus.Available
            };
            var created = await _ticketRepository.AddAsync(ticket);
            tickets.Add(created);
        }

        _logger.LogInformation("Se crearon {Quantity} tickets para evento {EventId}", command.Quantity, command.EventId);
        return tickets.Select(GetTicketsByEventQueryHandler.MapToDto);
    }
}
