namespace CrudService.Infrastructure.Services;

using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

public class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;

    public LogEmailSender(ILogger<LogEmailSender> logger)
    {
        _logger = logger;
    }

    public Task<EmailSendResult> SendOpportunityNotificationAsync(
        string buyerEmail,
        string eventName,
        DateTime expiresAt)
    {
        _logger.LogInformation(
            "[EMAIL STUB] To={BuyerEmail}, Event={EventName}, ExpiresAt={ExpiresAt}. " +
            "Content: Your waitlist opportunity for '{EventName}' is active. " +
            "Your ticket is temporarily reserved until {ExpiresAt:HH:mm UTC}. " +
            "Please check your status in the application. " +
            "This is an informational notice and not the official source of status.",
            buyerEmail, eventName, expiresAt, eventName, expiresAt);

        return Task.FromResult(new EmailSendResult(true, null));
    }
}
