using MsPaymentService.Application.Dtos;

namespace MsPaymentService.Application.Interfaces;

public interface IPaymentEventHandler
{
    string QueueName { get; }
    Task<ValidationResult> HandleAsync(string json, CancellationToken cancellationToken = default);
}
