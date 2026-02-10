using MsPaymentService.Worker.Models.Entities;

namespace MsPaymentService.Worker.Services;

public interface ITicketStateService
{
    Task<bool> TransitionToPaidAsync(long ticketId, string providerRef);
    Task<bool> TransitionToReleasedAsync(long ticketId, string reason);
    Task RecordHistoryAsync(long ticketId, TicketStatus oldStatus, TicketStatus newStatus, string reason);
}