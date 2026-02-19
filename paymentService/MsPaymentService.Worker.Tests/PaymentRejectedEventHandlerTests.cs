using System.Text.Json;
using Microsoft.Extensions.Options;
using MsPaymentService.Worker.Configurations;
using MsPaymentService.Worker.Handlers;
using MsPaymentService.Worker.Messaging;
using MsPaymentService.Worker.Models.DTOs;
using MsPaymentService.Worker.Models.Events;
using MsPaymentService.Worker.Services;
using NSubstitute;
using Xunit;

namespace MsPaymentService.Worker.Tests;

public class PaymentRejectedEventHandlerTests
{
    private readonly IPaymentValidationService _validationService;
    private readonly IStatusChangedPublisher _statusPublisher;
    private readonly PaymentRejectedEventHandler _sut;

    public PaymentRejectedEventHandlerTests()
    {
        _validationService = Substitute.For<IPaymentValidationService>();
        _statusPublisher = Substitute.For<IStatusChangedPublisher>();
        var settings = Options.Create(new RabbitMQSettings
        {
            ApprovedQueueName = "ticket.payments.approved",
            RejectedQueueName = "ticket.payments.rejected"
        });
        _sut = new PaymentRejectedEventHandler(_validationService, _statusPublisher, settings);
    }

    [Fact]
    public void QueueName_ReturnsRejectedQueueName()
    {
        Assert.Equal("ticket.payments.rejected", _sut.QueueName);
    }

    [Fact]
    public async Task HandleAsync_ValidJson_DelegatesToValidationService()
    {
        var evt = new PaymentRejectedEvent
        {
            TicketId = 1,
            PaymentId = 10,
            RejectionReason = "Insufficient funds",
            RejectedAt = DateTime.UtcNow,
            EventId = 100,
            EventTimestamp = DateTime.UtcNow
        };
        var json = JsonSerializer.Serialize(evt);
        _validationService.ValidateAndProcessRejectedPaymentAsync(Arg.Any<PaymentRejectedEvent>())
            .Returns(ValidationResult.Success());

        var result = await _sut.HandleAsync(json);

        Assert.True(result.IsSuccess);
        await _validationService.Received(1)
            .ValidateAndProcessRejectedPaymentAsync(Arg.Is<PaymentRejectedEvent>(e => e.TicketId == 1));
    }

    [Fact]
    public async Task HandleAsync_NullDeserialization_ReturnsFailure()
    {
        var result = await _sut.HandleAsync("null");

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid JSON", result.FailureReason!);
    }
}
