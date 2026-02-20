using CrudService.Application.Dtos;
using CrudService.Application.UseCases.Tickets.GetTicketsByEvent;
using CrudService.Domain.Entities;
using CrudService.Domain.Exceptions;
using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CrudService.Application.UseCases.Tickets.ReleaseTicket;

public class ReleaseTicketCommandHandler
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketHistoryRepository _historyRepository;
    private readonly ILogger<ReleaseTicketCommandHandler> _logger;

    public ReleaseTicketCommandHandler(
        ITicketRepository ticketRepository,
        ITicketHistoryRepository historyRepository,
        ILogger<ReleaseTicketCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _historyRepository = historyRepository;
        _logger = logger;
    }

    public async Task<TicketDto> HandleAsync(ReleaseTicketCommand command)
    {
        var ticket = await _ticketRepository.GetByIdAsync(command.Id)
            ?? throw new TicketNotFoundException(command.Id);

        var oldStatus = ticket.Status;
        ticket.Status = TicketStatus.Available;
        ticket.ReservedAt = null;
        ticket.ExpiresAt = null;
        ticket.ReservedBy = null;
        ticket.OrderId = null;
        ticket.Version++;

        var history = new TicketHistory
        {
            TicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus = TicketStatus.Available,
            Reason = command.Reason ?? "Ticket liberado"
        };

        await _historyRepository.AddAsync(history);
        var updated = await _ticketRepository.UpdateAsync(ticket);

        _logger.LogInformation("Ticket {TicketId} liberado. Razón: {Reason}", command.Id, command.Reason ?? "No especificada");
        return GetTicketsByEventQueryHandler.MapToDto(updated);
    }
}
