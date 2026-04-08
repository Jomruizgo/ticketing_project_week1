namespace CrudService.Domain.Interfaces;

public record EmailSendResult(bool Success, string? FailureReason);

public interface IEmailSender
{
    Task<EmailSendResult> SendOpportunityNotificationAsync(
        string buyerEmail,
        string eventName,
        DateTime expiresAt);
}
