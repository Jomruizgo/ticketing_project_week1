using Microsoft.EntityFrameworkCore;
using MsPaymentService.Worker.Data;
using MsPaymentService.Worker.Models.Entities;
using MsPaymentService.Worker.Repositories;

namespace MsPaymentService.Worker.Services;

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
            // 1. Obtener ticket con lock pesimista
            var ticket = await _ticketRepository.GetByIdForUpdateAsync(ticketId);
            
            if (ticket == null || ticket.Status != TicketStatus.Reserved)
            {
                _logger.LogWarning(
                    "Cannot transition to paid - invalid state. TicketId: {TicketId}, Status: {Status}",
                    ticketId, ticket?.Status);
                return false;
            }
            
            // 2. Actualizar ticket
            var oldStatus = ticket.Status;
            ticket.Status = TicketStatus.Paid;
            ticket.PaidAt = DateTime.UtcNow;
            ticket.Version++; // Optimistic concurrency
            
            await _ticketRepository.UpdateAsync(ticket);
            
            // 3. Actualizar payment
            var payment = await _paymentRepository.GetByTicketIdAsync(ticketId);
            if (payment != null)
            {
                payment.Status = PaymentStatus.Approved;
                payment.ProviderRef = providerRef;
                payment.UpdatedAt = DateTime.UtcNow;
                
                await _paymentRepository.UpdateAsync(payment);
            }
            
            // 4. Registrar en historial
            await RecordHistoryAsync(ticketId, oldStatus, TicketStatus.Paid, "Payment approved");
            
            // 5. Commit transacción
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
            // 1. Obtener ticket
            var ticket = await _ticketRepository.GetByIdForUpdateAsync(ticketId);
            
            if (ticket == null)
            {
                _logger.LogWarning("Ticket {TicketId} not found", ticketId);
                return false;
            }
            
            // 2. Actualizar ticket
            var oldStatus = ticket.Status;
            ticket.Status = TicketStatus.Released;
            ticket.Version++; // Optimistic concurrency
            
            await _ticketRepository.UpdateAsync(ticket);
            
            // 3. Actualizar payment si existe
            var payment = await _paymentRepository.GetByTicketIdAsync(ticketId);
            if (payment != null && payment.Status == PaymentStatus.Pending)
            {
                payment.Status = reason.Contains("TTL") ? PaymentStatus.Expired : PaymentStatus.Failed;
                payment.UpdatedAt = DateTime.UtcNow;
                
                await _paymentRepository.UpdateAsync(payment);
            }
            
            // 4. Registrar en historial
            await RecordHistoryAsync(ticketId, oldStatus, TicketStatus.Released, reason);
            
            // 5. Commit transacción
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