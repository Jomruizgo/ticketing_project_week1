namespace CrudService.Domain.Entities;

using CrudService.Domain.Enums;

public class NotificationDelivery
{
    public long Id { get; set; }
    public long WaitlistOpportunityId { get; set; }
    public string Channel { get; set; } = "email";
    public NotificationDeliveryStatus Status { get; set; } = NotificationDeliveryStatus.Pending;
    public DateTime SentAt { get; set; }
    public string? FailureReason { get; set; }

    public WaitlistOpportunity WaitlistOpportunity { get; set; } = null!;

    public void MarkAsSent(DateTime sentAt)
    {
        EnsureNotTerminal();
        Status = NotificationDeliveryStatus.Sent;
        SentAt = sentAt;
        FailureReason = null;
    }

    public void MarkAsFailed(DateTime sentAt, string failureReason)
    {
        EnsureNotTerminal();
        Status = NotificationDeliveryStatus.Failed;
        SentAt = sentAt;
        FailureReason = failureReason;
    }

    private void EnsureNotTerminal()
    {
        if (Status is NotificationDeliveryStatus.Sent or NotificationDeliveryStatus.Failed)
            throw new InvalidOperationException(
                $"Cannot transition NotificationDelivery {Id} from terminal state '{Status}'.");
    }
}
