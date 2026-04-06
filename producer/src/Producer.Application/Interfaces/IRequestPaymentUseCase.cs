using Producer.Application.UseCases.RequestPayment;

namespace Producer.Application.Interfaces;

public interface IRequestPaymentUseCase
{
    Task<RequestPaymentResponse> HandleAsync(RequestPaymentCommand command, CancellationToken ct = default);
}