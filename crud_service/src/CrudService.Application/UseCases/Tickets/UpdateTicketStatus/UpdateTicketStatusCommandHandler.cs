using CrudService.Application.Dtos;
using CrudService.Application.Exceptions;
using CrudService.Application.UseCases.Tickets.GetTicketsByEvent;
using CrudService.Domain.Entities;
using CrudService.Domain.Exceptions;
using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace CrudService.Application.UseCases.Tickets.UpdateTicketStatus;

public class UpdateTicketStatusCommandHandler
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketHistoryRepository _historyRepository;
    private readonly ILogger<UpdateTicketStatusCommandHandler> _logger;

    public UpdateTicketStatusCommandHandler(
        ITicketRepository ticketRepository,
        ITicketHistoryRepository historyRepository,
        ILogger<UpdateTicketStatusCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _historyRepository = historyRepository;
        _logger = logger;
    }

    public async Task<TicketDto> HandleAsync(UpdateTicketStatusCommand command)
    {
        var ticket = await _ticketRepository.GetByIdAsync(command.Id)
            ?? throw new TicketNotFoundException(command.Id);

        if (!Enum.TryParse<TicketStatus>(command.NewStatus, ignoreCase: true, out var status))
            throw new InvalidTicketStatusException(command.NewStatus);

        var oldStatus = ticket.Status;
        ticket.Status = status;
        ticket.Version++;

        var history = new TicketHistory
        {
            TicketId = ticket.Id,
            OldStatus = oldStatus,
            NewStatus = status,
            Reason = command.Reason
        };

        await _historyRepository.AddAsync(history);
        var updated = await _ticketRepository.UpdateAsync(ticket);

        _logger.LogInformation("Ticket {TicketId} cambió de {OldStatus} a {NewStatus}", command.Id, oldStatus, status);
        return GetTicketsByEventQueryHandler.MapToDto(updated);
    }
}
