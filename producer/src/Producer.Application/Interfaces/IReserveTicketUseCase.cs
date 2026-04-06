using Producer.Application.UseCases.ReserveTicket;

namespace Producer.Application.Interfaces;

public interface IReserveTicketUseCase
{
    Task<ReserveTicketResponse> HandleAsync(ReserveTicketCommand command, CancellationToken ct = default);
}