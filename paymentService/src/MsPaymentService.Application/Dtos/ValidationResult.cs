namespace MsPaymentService.Application.Dtos;

public class ValidationResult
{
    public bool IsSuccess { get; set; }
    public string? FailureReason { get; set; }
    public bool IsAlreadyProcessed { get; set; }

    public static ValidationResult Success() => new() { IsSuccess = true };

    public static ValidationResult Failure(string reason) => new()
    {
        IsSuccess = false,
        FailureReason = reason
    };

    public static ValidationResult AlreadyProcessed() => new()
    {
        IsSuccess = false,
        IsAlreadyProcessed = true,
        FailureReason = "Event already processed"
    };
}
