using System.Text;
using System.Text.Json;
using CrudService.Infrastructure.Messaging;
using CrudService.Infrastructure.Sse;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace CrudService.Infrastructure.Tests.Sse;

public class SseNotificationConsumerTests
{
    private readonly IWaitlistSseNotifier _notifier = Substitute.For<IWaitlistSseNotifier>();
    private readonly ILogger<SseNotificationConsumer> _logger =
        Substitute.For<ILogger<SseNotificationConsumer>>();

    [Fact]
    public void ProcessActivatedMessage_CallsSendEventAsync_WithCorrectPayload()
    {
        var consumer = new SseNotificationConsumer(_notifier, null!, _logger);

        var message = new
        {
            opportunityId = 5L,
            waitlistEntryId = 1L,
            ticketId = 100L,
            eventId = 42L,
            buyerEmail = "buyer@test.com",
            activatedAt = "2026-04-07T11:00:00Z",
            expiresAt = "2026-04-07T11:15:00Z"
        };

        var json = JsonSerializer.Serialize(message);

        consumer.ProcessMessage(json, "waitlist.opportunity.activated");

        _notifier.Received(1).SendEventAsync(
            "buyer@test.com",
            "opportunity_activated",
            Arg.Is<string>(payload =>
                payload.Contains("\"opportunityId\":5") &&
                payload.Contains("\"ticketId\":100") &&
                payload.Contains("\"eventId\":42") &&
                payload.Contains("\"expiresAt\"") &&
                payload.Contains("\"remainingMinutes\"")));
    }

    [Fact]
    public void ProcessActivatedMessage_CalculatesRemainingMinutesAsFloor()
    {
        var consumer = new SseNotificationConsumer(_notifier, null!, _logger);

        var expiresAt = DateTime.UtcNow.AddMinutes(7.8);
        var message = new
        {
            opportunityId = 1L,
            waitlistEntryId = 1L,
            ticketId = 1L,
            eventId = 1L,
            buyerEmail = "buyer@test.com",
            activatedAt = DateTime.UtcNow.ToString("O"),
            expiresAt = expiresAt.ToString("O")
        };

        var json = JsonSerializer.Serialize(message);

        consumer.ProcessMessage(json, "waitlist.opportunity.activated");

        _notifier.Received(1).SendEventAsync(
            "buyer@test.com",
            "opportunity_activated",
            Arg.Is<string>(payload => payload.Contains("\"remainingMinutes\":7")));
    }

    [Fact]
    public void ProcessMessage_MalformedJson_LogsErrorAndDoesNotThrow()
    {
        var consumer = new SseNotificationConsumer(_notifier, null!, _logger);

        var ex = Record.Exception(() =>
            consumer.ProcessMessage("not-valid-json{{{", "waitlist.opportunity.activated"));

        Assert.Null(ex);
        _notifier.DidNotReceive().SendEventAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void ProcessExpiredMessage_CallsSendEventAsync_WithCorrectPayload()
    {
        var consumer = new SseNotificationConsumer(_notifier, null!, _logger);

        var message = new
        {
            opportunityId = 10L,
            waitlistEntryId = 2L,
            ticketId = 200L,
            eventId = 55L,
            buyerEmail = "expired@test.com",
            activatedAt = "2026-04-07T11:00:00Z",
            expiresAt = "2026-04-07T11:15:00Z"
        };

        var json = JsonSerializer.Serialize(message);

        consumer.ProcessMessage(json, "waitlist.opportunity.expired");

        _notifier.Received(1).SendEventAsync(
            "expired@test.com",
            "opportunity_expired",
            Arg.Is<string>(payload =>
                payload.Contains("\"opportunityId\":10") &&
                payload.Contains("\"eventId\":55") &&
                payload.Contains("\"reason\":\"timeout\"")));
    }

    [Fact]
    public void Constructor_OnlyRequiresNotifierAndLogger_NoRepositoryDependencies()
    {
        // SseNotificationConsumer should NOT depend on any repository — it is read-only (FR-006)
        var constructorParams = typeof(SseNotificationConsumer)
            .GetConstructors()
            .First()
            .GetParameters()
            .Select(p => p.ParameterType)
            .ToList();

        Assert.DoesNotContain(constructorParams,
            t => t.Name.Contains("Repository", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProcessMessage_SendEventAsyncThrows_DoesNotPropagateException()
    {
        _notifier.SendEventAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromException(new InvalidOperationException("hub failure")));

        var consumer = new SseNotificationConsumer(_notifier, null!, _logger);

        var message = new
        {
            opportunityId = 1L,
            waitlistEntryId = 1L,
            ticketId = 1L,
            eventId = 1L,
            buyerEmail = "fail@test.com",
            activatedAt = "2026-04-07T11:00:00Z",
            expiresAt = "2026-04-07T11:15:00Z"
        };

        var json = JsonSerializer.Serialize(message);

        var ex = Record.Exception(() =>
            consumer.ProcessMessage(json, "waitlist.opportunity.activated"));

        Assert.Null(ex);
    }
}
