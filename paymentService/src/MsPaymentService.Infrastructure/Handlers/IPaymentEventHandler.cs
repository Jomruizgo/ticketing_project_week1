using MsPaymentService.Application.Dtos;

namespace MsPaymentService.Infrastructure.Handlers;

public interface IPaymentEventHandler
{
    string QueueName { get; }
    Task<ValidationResult> HandleAsync(string json, CancellationToken cancellationToken = default);
}
