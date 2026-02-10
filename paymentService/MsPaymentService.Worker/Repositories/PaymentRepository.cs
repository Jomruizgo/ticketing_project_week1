using Microsoft.EntityFrameworkCore;
using MsPaymentService.Worker.Data;
using MsPaymentService.Worker.Models.Entities;

namespace MsPaymentService.Worker.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByTicketIdAsync(long ticketId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.TicketId == ticketId);
    }

    public async Task<Payment?> GetByIdAsync(long id)
    {
        return await _context.Payments
            .Include(p => p.Ticket)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

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

    public async Task<Payment> CreateAsync(Payment payment)
    {
        payment.CreatedAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;
        
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }
}