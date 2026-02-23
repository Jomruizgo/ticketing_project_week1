using MsPaymentService.Application.Dtos;

namespace MsPaymentService.Application.Interfaces;

public interface IPaymentEventDispatcher
{
    Task<ValidationResult?> DispatchAsync(string queueName, string json, CancellationToken cancellationToken = default);
}
