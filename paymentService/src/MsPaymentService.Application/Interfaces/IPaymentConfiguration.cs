namespace MsPaymentService.Application.Interfaces;

public interface IPaymentConfiguration
{
    int ReservationTtlMinutes { get; }
}
