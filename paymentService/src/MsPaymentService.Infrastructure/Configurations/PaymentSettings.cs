using MsPaymentService.Application.Interfaces;

namespace MsPaymentService.Infrastructure.Configurations;

public class PaymentSettings : IPaymentConfiguration
{
    public int ReservationTtlMinutes { get; set; } = 5;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
}
