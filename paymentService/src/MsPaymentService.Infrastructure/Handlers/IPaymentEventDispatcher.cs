using MsPaymentService.Application.Dtos;

namespace MsPaymentService.Infrastructure.Handlers;

public interface IPaymentEventDispatcher
{
    Task<ValidationResult?> DispatchAsync(string queueName, string json, CancellationToken cancellationToken = default);
}
