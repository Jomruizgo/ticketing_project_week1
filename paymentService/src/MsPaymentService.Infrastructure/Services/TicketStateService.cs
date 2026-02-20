using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MsPaymentService.Application.Interfaces;
using MsPaymentService.Domain.Entities;
using MsPaymentService.Domain.Interfaces;
using MsPaymentService.Infrastructure.Persistence;

namespace MsPaymentService.Infrastructure.Services;

public class TicketStateService : ITicketStateService
{
    private readonly PaymentDbContext _dbContext;
    private readonly ITicketRepository _ticketRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITicketHistoryRepository _historyRepository;
    private readonly ILogger<TicketStateService> _logger;

    public TicketStateService(
        PaymentDbContext dbContext,
        ITicketRepository ticketRepository,
        IPaymentRepository paymentRepository,
        ITicketHistoryRepository historyRepository,
        ILogger<TicketStateService> logger)
    {
        _dbContext = dbContext;
        _ticketRepository = ticketRepository;
        _paymentRepository = paymentRepository;
        _historyRepository = historyRepository;
        _logger = logger;
    }

    public async Task<bool> TransitionToPaidAsync(long ticketId, string providerRef)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var ticket = await _ticketRepository.GetByIdForUpdateAsync(ticketId);

            if (ticket == null || ticket.Status != TicketStatus.reserved)
            {
                _logger.LogWarning(
                    "Cannot transition to paid - invalid state. TicketId: {TicketId}, Status: {Status}",
                    ticketId, ticket?.Status);
                return false;
            }

            var oldStatus = ticket.Status;
            ticket.Status = TicketStatus.paid;
            ticket.PaidAt = DateTime.UtcNow;

            var updated = await _ticketRepository.UpdateAsync(ticket);

            if (!updated)
            {
                var current = await _ticketRepository.GetByIdAsync(ticketId);

                if (current != null && current.Status == TicketStatus.released)
                {
                    _logger.LogInformation("Ticket {TicketId} already released (idempotent event).", ticketId);
                    return true;
                }

                _logger.LogWarning("Failed to update ticket {TicketId} - real concurrent modification", ticketId);
                return false;
            }

            var payment = await _paymentRepository.GetByTicketIdAsync(ticketId);
            if (payment != null)
            {
                payment.Status = PaymentStatus.approved;
                payment.ProviderRef = providerRef;
                payment.UpdatedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);
            }

            await RecordHistoryAsync(ticketId, oldStatus, TicketStatus.paid, "Payment approved");

            await transaction.CommitAsync();

            _logger.LogInformation(
                "Ticket {TicketId} successfully transitioned to Paid with provider ref {ProviderRef}",
                ticketId, providerRef);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error transitioning ticket {TicketId} to paid", ticketId);
            throw;
        }
    }

    public async Task<bool> TransitionToReleasedAsync(long ticketId, string reason)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var ticket = await _ticketRepository.GetByIdForUpdateAsync(ticketId);

            if (ticket == null)
            {
                _logger.LogWarning("Ticket {TicketId} not found", ticketId);
                return false;
            }

            var oldStatus = ticket.Status;
            ticket.Status = TicketStatus.released;

            var updated = await _ticketRepository.UpdateAsync(ticket);

            if (!updated)
            {
                var current = await _ticketRepository.GetByIdAsync(ticketId);

                if (current != null && current.Status == TicketStatus.released)
                {
                    _logger.LogInformation(
                        "Ticket {TicketId} - {Status} already released (idempotent event).",
                        ticketId, current.Status);
                    return true;
                }

                _logger.LogWarning("Failed to update ticket {TicketId} - real concurrent modification", ticketId);
                return false;
            }

            var payment = await _paymentRepository.GetByTicketIdAsync(ticketId);
            if (payment != null && payment.Status == PaymentStatus.pending)
            {
                payment.Status = reason.Contains("TTL") ? PaymentStatus.expired : PaymentStatus.failed;
                payment.UpdatedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);
            }

            await RecordHistoryAsync(ticketId, oldStatus, TicketStatus.released, reason);

            await transaction.CommitAsync();

            _logger.LogInformation(
                "Ticket {TicketId} successfully transitioned to Released. Reason: {Reason}",
                ticketId, reason);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error transitioning ticket {TicketId} to released", ticketId);
            throw;
        }
    }

    public async Task RecordHistoryAsync(long ticketId, TicketStatus oldStatus, TicketStatus newStatus, string reason)
    {
        var history = new TicketHistory
        {
            TicketId = ticketId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedAt = DateTime.UtcNow,
            Reason = reason
        };

        await _historyRepository.AddAsync(history);

        _logger.LogDebug(
            "Recorded history for ticket {TicketId}: {OldStatus} -> {NewStatus}. Reason: {Reason}",
            ticketId, oldStatus, newStatus, reason);
    }
}
