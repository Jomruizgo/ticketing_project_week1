using CrudService.Infrastructure.Persistence;
using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CrudService.Infrastructure.Persistence.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly TicketingDbContext _context;

    public PaymentRepository(TicketingDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(long id)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Payment> AddAsync(Payment payment)
    {
        await _context.Payments.AddAsync(payment);
        await SaveChangesAsync();
        return payment;
    }

    public async Task<Payment> UpdateAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        await SaveChangesAsync();
        return payment;
    }

    public async Task<IEnumerable<Payment>> GetByTicketIdAsync(long ticketId)
    {
        return await _context.Payments
            .Where(p => p.TicketId == ticketId)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
