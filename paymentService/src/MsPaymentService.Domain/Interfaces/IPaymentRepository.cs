using MsPaymentService.Domain.Entities;

namespace MsPaymentService.Domain.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByTicketIdAsync(long ticketId);
    Task<Payment?> GetByIdAsync(long id);
    Task<bool> UpdateAsync(Payment payment);
    Task<Payment> CreateAsync(Payment payment);
}
