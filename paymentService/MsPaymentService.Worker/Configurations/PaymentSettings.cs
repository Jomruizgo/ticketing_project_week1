namespace MsPaymentService.Worker.Configurations;

public class PaymentSettings
{
    public int ReservationTtlMinutes { get; set; } = 5;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
}