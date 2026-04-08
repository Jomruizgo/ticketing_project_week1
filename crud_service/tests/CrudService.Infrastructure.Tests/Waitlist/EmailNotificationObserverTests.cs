using CrudService.Domain.Entities;
using CrudService.Domain.Enums;
using CrudService.Domain.Events;
using CrudService.Domain.Interfaces;
using CrudService.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Core;
using Xunit;

namespace CrudService.Infrastructure.Tests.Waitlist;

[Trait("Category", "Unit")]
public class EmailNotificationObserverTests
{
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly INotificationDeliveryRepository _deliveryRepo = Substitute.For<INotificationDeliveryRepository>();
    private readonly ILogger<EmailNotificationObserver> _logger = Substitute.For<ILogger<EmailNotificationObserver>>();

    private EmailNotificationObserver CreateObserver() =>
        new(_emailSender, _deliveryRepo, _logger);

    private static OpportunityActivatedEvent CreateTestEvent(
        string buyerEmail = "buyer@example.com",
        string eventName = "Concierto Rock 2026",
        long opportunityId = 1,
        long eventId = 42) =>
        new(
            OpportunityId: opportunityId,
            EventId: eventId,
            EventName: eventName,
            BuyerEmail: buyerEmail,
            ActivatedAt: DateTime.UtcNow,
            ExpiresAt: DateTime.UtcNow.AddMinutes(15));

    private void SetupRepoReturnsDelivery()
    {
        _deliveryRepo.AddAsync(Arg.Any<NotificationDelivery>())
            .Returns(ci => ci.Arg<NotificationDelivery>());
    }

    // ── T015: TC-HU5-01 — Correo enviado al activarse la oportunidad ──────

    [Fact]
    public async Task OnOpportunityActivated_InvokesEmailSender_WithCorrectArguments()
    {
        // Arrange
        SetupRepoReturnsDelivery();
        _emailSender.SendOpportunityNotificationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(new EmailSendResult(true, null));

        var activatedEvent = CreateTestEvent();
        var observer = CreateObserver();

        // Act
        await observer.OnOpportunityActivatedAsync(activatedEvent);

        // Assert
        await _emailSender.Received(1).SendOpportunityNotificationAsync(
            "buyer@example.com",
            "Concierto Rock 2026",
            activatedEvent.ExpiresAt);
    }

    // ── T016: TC-HU5-02 — Contenido mínimo completo (FR-002, 5 elementos) ──

    [Fact]
    public async Task OnOpportunityActivated_PassesSanitizedEventName_WithoutHtml()
    {
        // Arrange
        SetupRepoReturnsDelivery();
        _emailSender.SendOpportunityNotificationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(new EmailSendResult(true, null));

        var activatedEvent = CreateTestEvent(eventName: "<script>alert('xss')</script>Concert");
        var observer = CreateObserver();

        // Act
        await observer.OnOpportunityActivatedAsync(activatedEvent);

        // Assert — eventName passed to sender must NOT contain HTML tags
        await _emailSender.Received(1).SendOpportunityNotificationAsync(
            Arg.Any<string>(),
            Arg.Is<string>(name => !name.Contains('<') && !name.Contains('>')),
            Arg.Any<DateTime>());
    }

    [Fact]
    public async Task OnOpportunityActivated_PassesExpiresAt_Within15Minutes()
    {
        // Arrange
        SetupRepoReturnsDelivery();
        _emailSender.SendOpportunityNotificationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(new EmailSendResult(true, null));

        var activatedEvent = CreateTestEvent();
        var observer = CreateObserver();

        // Act
        await observer.OnOpportunityActivatedAsync(activatedEvent);

        // Assert — expiresAt must be the event's ExpiresAt (15-min validity)
        await _emailSender.Received(1).SendOpportunityNotificationAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            activatedEvent.ExpiresAt);
    }

    // ── T016b: FR-008 — Correo sin enlaces de acción directa ──────────────

    [Fact]
    public async Task OnOpportunityActivated_NoActionLinks_PassedToSender()
    {
        // Arrange
        SetupRepoReturnsDelivery();
        _emailSender.SendOpportunityNotificationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(new EmailSendResult(true, null));

        var activatedEvent = CreateTestEvent();
        var observer = CreateObserver();

        // Act
        await observer.OnOpportunityActivatedAsync(activatedEvent);

        // Assert — the observer delegates content generation to IEmailSender;
        // the eventName must not contain payment/confirmation URLs
        await _emailSender.Received(1).SendOpportunityNotificationAsync(
            Arg.Any<string>(),
            Arg.Is<string>(name => !name.Contains("http://") && !name.Contains("https://")),
            Arg.Any<DateTime>());
    }

    // ── T017: Envío exitoso registra status = sent ────────────────────────

    [Fact]
    public async Task OnOpportunityActivated_SuccessfulSend_RecordsStatusSent()
    {
        // Arrange
        NotificationDeliveryStatus? statusAtAdd = null;
        _deliveryRepo.AddAsync(Arg.Any<NotificationDelivery>())
            .Returns(ci =>
            {
                var d = ci.Arg<NotificationDelivery>();
                statusAtAdd = d.Status;
                return d;
            });

        _emailSender.SendOpportunityNotificationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(new EmailSendResult(true, null));

        var activatedEvent = CreateTestEvent();
        var observer = CreateObserver();

        // Act
        await observer.OnOpportunityActivatedAsync(activatedEvent);

        // Assert — delivery created as Pending
        Assert.Equal(NotificationDeliveryStatus.Pending, statusAtAdd);

        // Assert — then updated to Sent
        await _deliveryRepo.Received(1).UpdateAsync(
            Arg.Is<NotificationDelivery>(d =>
                d.Status == NotificationDeliveryStatus.Sent &&
                d.FailureReason == null &&
                d.WaitlistOpportunityId == activatedEvent.OpportunityId));
    }

    // ── T018: Fallo del proveedor registra status = failed ────────────────

    [Fact]
    public async Task OnOpportunityActivated_ProviderFails_RecordsStatusFailed()
    {
        // Arrange
        SetupRepoReturnsDelivery();
        _emailSender.SendOpportunityNotificationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(new EmailSendResult(false, "provider error"));

        var activatedEvent = CreateTestEvent();
        var observer = CreateObserver();

        // Act
        await observer.OnOpportunityActivatedAsync(activatedEvent);

        // Assert — delivery updated to Failed with failure reason
        await _deliveryRepo.Received(1).UpdateAsync(
            Arg.Is<NotificationDelivery>(d =>
                d.Status == NotificationDeliveryStatus.Failed &&
                d.FailureReason == "provider error"));
    }

    // ── T018b: Timeout registra status = failed con reason "timeout" ──────

    [Fact]
    public async Task OnOpportunityActivated_ProviderTimeout_RecordsStatusFailedWithTimeout()
    {
        // Arrange
        SetupRepoReturnsDelivery();
        _emailSender.SendOpportunityNotificationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(new EmailSendResult(false, "timeout"));

        var activatedEvent = CreateTestEvent();
        var observer = CreateObserver();

        // Act
        await observer.OnOpportunityActivatedAsync(activatedEvent);

        // Assert
        await _deliveryRepo.Received(1).UpdateAsync(
            Arg.Is<NotificationDelivery>(d =>
                d.Status == NotificationDeliveryStatus.Failed &&
                d.FailureReason == "timeout"));
    }

    // ── T018c: FR-005 — Inmutabilidad post-terminal ──────────────────────

    [Fact]
    public void NotificationDelivery_TerminalState_RejectsSecondUpdate()
    {
        // Arrange
        var delivery = new NotificationDelivery
        {
            Id = 1,
            WaitlistOpportunityId = 10,
            Channel = "email",
            Status = NotificationDeliveryStatus.Pending,
            SentAt = DateTime.UtcNow
        };
        delivery.MarkAsSent(DateTime.UtcNow);

        // Act & Assert — second transition must throw
        Assert.Throws<InvalidOperationException>(() =>
            delivery.MarkAsFailed(DateTime.UtcNow, "should not work"));
    }

    [Fact]
    public void NotificationDelivery_FailedState_RejectsSecondUpdate()
    {
        // Arrange
        var delivery = new NotificationDelivery
        {
            Id = 2,
            WaitlistOpportunityId = 20,
            Channel = "email",
            Status = NotificationDeliveryStatus.Pending,
            SentAt = DateTime.UtcNow
        };
        delivery.MarkAsFailed(DateTime.UtcNow, "first failure");

        // Act & Assert — second transition must throw
        Assert.Throws<InvalidOperationException>(() =>
            delivery.MarkAsSent(DateTime.UtcNow));
    }

    // ── T022: TC-HU5-03 — Fallo no afecta oportunidad (no lanza excepción) ──

    [Fact]
    public async Task OnOpportunityActivated_ProviderFails_DoesNotThrow()
    {
        // Arrange
        SetupRepoReturnsDelivery();
        _emailSender.SendOpportunityNotificationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(new EmailSendResult(false, "SMTP down"));

        var activatedEvent = CreateTestEvent();
        var observer = CreateObserver();

        // Act & Assert — must NOT throw
        var exception = await Record.ExceptionAsync(() =>
            observer.OnOpportunityActivatedAsync(activatedEvent));

        Assert.Null(exception);
    }

    // ── T022b: Excepción inesperada no propaga ───────────────────────────

    [Fact]
    public async Task OnOpportunityActivated_SenderThrowsException_DoesNotPropagate()
    {
        // Arrange
        SetupRepoReturnsDelivery();
        _emailSender.SendOpportunityNotificationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns<EmailSendResult>(_ => throw new HttpRequestException("Connection refused"));

        var activatedEvent = CreateTestEvent();
        var observer = CreateObserver();

        // Act & Assert — exception must be captured, NOT propagated
        var exception = await Record.ExceptionAsync(() =>
            observer.OnOpportunityActivatedAsync(activatedEvent));

        Assert.Null(exception);

        // Verify failure was recorded
        await _deliveryRepo.Received(1).UpdateAsync(
            Arg.Is<NotificationDelivery>(d =>
                d.Status == NotificationDeliveryStatus.Failed &&
                d.FailureReason == "Connection refused"));
    }

    // ── T023: Fallo de auditoría no propaga ──────────────────────────────

    [Fact]
    public async Task OnOpportunityActivated_RepoThrows_DoesNotPropagate()
    {
        // Arrange
        _deliveryRepo.AddAsync(Arg.Any<NotificationDelivery>())
            .Returns<NotificationDelivery>(_ => throw new InvalidOperationException("DB connection lost"));

        var activatedEvent = CreateTestEvent();
        var observer = CreateObserver();

        // Act & Assert — repo failure must not propagate
        var exception = await Record.ExceptionAsync(() =>
            observer.OnOpportunityActivatedAsync(activatedEvent));

        Assert.Null(exception);
    }

    // ── T024: TC-HU5-04 — Pending persisted before email sent ────────────

    [Fact]
    public async Task OnOpportunityActivated_PendingPersistedBeforeEmailSent()
    {
        // Arrange
        var callOrder = new List<string>();

        _deliveryRepo.AddAsync(Arg.Any<NotificationDelivery>())
            .Returns(ci =>
            {
                callOrder.Add("AddAsync");
                return ci.Arg<NotificationDelivery>();
            });

        _emailSender.SendOpportunityNotificationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(ci =>
            {
                callOrder.Add("SendEmail");
                return new EmailSendResult(true, null);
            });

        var activatedEvent = CreateTestEvent();
        var observer = CreateObserver();

        // Act
        await observer.OnOpportunityActivatedAsync(activatedEvent);

        // Assert — AddAsync must be called BEFORE SendEmail
        Assert.Equal(2, callOrder.Count);
        Assert.Equal("AddAsync", callOrder[0]);
        Assert.Equal("SendEmail", callOrder[1]);
    }
}
