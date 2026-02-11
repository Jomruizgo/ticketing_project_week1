using Microsoft.EntityFrameworkCore;
using MsPaymentService.Worker.Data;
using MsPaymentService.Worker.Models.Entities;

namespace MsPaymentService.Worker.Repositories;

/// <summary>
/// Implementación del repositorio de pagos usando Entity Framework Core.
/// Proporciona operaciones CRUD sobre la tabla de pagos.
/// </summary>
public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="PaymentRepository"/>.
    /// </summary>
    /// <param name="context">Contexto de base de datos de Entity Framework.</param>
    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<Payment?> GetByTicketIdAsync(long ticketId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.TicketId == ticketId);
    }

    /// <inheritdoc/>
    public async Task<Payment?> GetByIdAsync(long id)
    {
        return await _context.Payments
            .Include(p => p.Ticket)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(Payment payment)
    {
        try
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<Payment> CreateAsync(Payment payment)
    {
        payment.CreatedAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;
        
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }
}