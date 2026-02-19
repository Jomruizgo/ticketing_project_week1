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

public class PaymentApprovedEventHandlerTests
{
    private readonly IPaymentValidationService _validationService;
    private readonly IStatusChangedPublisher _statusPublisher;
    private readonly PaymentApprovedEventHandler _sut;

    public PaymentApprovedEventHandlerTests()
    {
        _validationService = Substitute.For<IPaymentValidationService>();
        _statusPublisher = Substitute.For<IStatusChangedPublisher>();
        var settings = Options.Create(new RabbitMQSettings
        {
            ApprovedQueueName = "ticket.payments.approved",
            RejectedQueueName = "ticket.payments.rejected"
        });
        _sut = new PaymentApprovedEventHandler(_validationService, _statusPublisher, settings);
    }

    [Fact]
    public void QueueName_ReturnsApprovedQueueName()
    {
        Assert.Equal("ticket.payments.approved", _sut.QueueName);
    }

    [Fact]
    public async Task HandleAsync_ValidJson_DelegatesToValidationService()
    {
        var evt = new PaymentApprovedEvent
        {
            TicketId = 1,
            EventId = 100,
            AmountCents = 5000,
            Currency = "USD",
            PaymentBy = "user@test.com",
            TransactionRef = "TXN-001",
            ApprovedAt = DateTime.UtcNow
        };
        var json = JsonSerializer.Serialize(evt);
        _validationService.ValidateAndProcessApprovedPaymentAsync(Arg.Any<PaymentApprovedEvent>())
            .Returns(ValidationResult.Success());

        var result = await _sut.HandleAsync(json);

        Assert.True(result.IsSuccess);
        await _validationService.Received(1)
            .ValidateAndProcessApprovedPaymentAsync(Arg.Is<PaymentApprovedEvent>(e => e.TicketId == 1));
    }

    [Fact]
    public async Task HandleAsync_InvalidJson_ThrowsJsonException()
    {
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(
            () => _sut.HandleAsync("not valid json {{{"));
    }

    [Fact]
    public async Task HandleAsync_NullDeserialization_ReturnsFailure()
    {
        var result = await _sut.HandleAsync("null");

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid JSON", result.FailureReason!);
    }
}
