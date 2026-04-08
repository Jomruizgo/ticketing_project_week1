namespace CrudService.Infrastructure.Services;

using System.Net;
using CrudService.Domain.Entities;
using CrudService.Domain.Enums;
using CrudService.Domain.Events;
using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

public class EmailNotificationObserver : IOpportunityObserver
{
    private readonly IEmailSender _emailSender;
    private readonly INotificationDeliveryRepository _deliveryRepo;
    private readonly ILogger<EmailNotificationObserver> _logger;

    public EmailNotificationObserver(
        IEmailSender emailSender,
        INotificationDeliveryRepository deliveryRepo,
        ILogger<EmailNotificationObserver> logger)
    {
        _emailSender = emailSender;
        _deliveryRepo = deliveryRepo;
        _logger = logger;
    }

    public async Task OnOpportunityActivatedAsync(OpportunityActivatedEvent activatedEvent)
    {
        try
        {
            var delivery = new NotificationDelivery
            {
                WaitlistOpportunityId = activatedEvent.OpportunityId,
                Channel = "email",
                Status = NotificationDeliveryStatus.Pending,
                SentAt = DateTime.UtcNow
            };

            await _deliveryRepo.AddAsync(delivery);

            var sanitizedEventName = SanitizeEventName(activatedEvent.EventName);

            EmailSendResult result;
            try
            {
                result = await _emailSender.SendOpportunityNotificationAsync(
                    activatedEvent.BuyerEmail,
                    sanitizedEventName,
                    activatedEvent.ExpiresAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected exception from IEmailSender for OpportunityId={OpportunityId}",
                    activatedEvent.OpportunityId);
                result = new EmailSendResult(false, ex.Message);
            }

            if (result.Success)
            {
                delivery.MarkAsSent(DateTime.UtcNow);
            }
            else
            {
                delivery.MarkAsFailed(DateTime.UtcNow, result.FailureReason ?? "unknown");
            }

            await _deliveryRepo.UpdateAsync(delivery);

            _logger.LogInformation(
                "Email notification for OpportunityId={OpportunityId}: {Status}",
                activatedEvent.OpportunityId, delivery.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process email notification for OpportunityId={OpportunityId}. " +
                "Opportunity remains active.",
                activatedEvent.OpportunityId);
        }
    }

    private static string SanitizeEventName(string eventName)
    {
        return WebUtility.HtmlEncode(eventName)
            .Replace("&lt;", "")
            .Replace("&gt;", "")
            .Replace("&amp;", "&")
            .Replace("&quot;", "\"")
            .Replace("&#39;", "'");
    }
}
