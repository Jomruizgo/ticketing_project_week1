using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MsPaymentService.Application.Dtos;
using MsPaymentService.Application.Events;
using MsPaymentService.Application.Interfaces;
using MsPaymentService.Application.UseCases.ProcessApprovedPayment;
using MsPaymentService.Domain.Entities;
using MsPaymentService.Domain.Interfaces;
using MsPaymentService.Infrastructure.Configurations;
using MsPaymentService.Infrastructure.Handlers;
using MsPaymentService.Infrastructure.Messaging;
using NSubstitute;
using Xunit;

namespace MsPaymentService.Application.Tests;

public class PaymentApprovedEventHandlerTests
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITicketStateService _stateService;
    private readonly IStatusChangedPublisher _statusPublisher;
    private readonly PaymentApprovedEventHandler _sut;

    public PaymentApprovedEventHandlerTests()
    {
        _ticketRepository = Substitute.For<ITicketRepository>();
        _paymentRepository = Substitute.For<IPaymentRepository>();
        _stateService = Substitute.For<ITicketStateService>();
        _statusPublisher = Substitute.For<IStatusChangedPublisher>();

        var commandHandler = new ProcessApprovedPaymentCommandHandler(
            _ticketRepository,
            _paymentRepository,
            _stateService,
            Substitute.For<ILogger<ProcessApprovedPaymentCommandHandler>>());

        var settings = Options.Create(new RabbitMQSettings
        {
            ApprovedQueueName = "ticket.payments.approved",
            RejectedQueueName = "ticket.payments.rejected"
        });

        _sut = new PaymentApprovedEventHandler(commandHandler, _statusPublisher, settings);
    }

    [Fact]
    public void QueueName_ReturnsApprovedQueueName()
    {
        Assert.Equal("ticket.payments.approved", _sut.QueueName);
    }

    [Fact]
    public async Task HandleAsync_ValidJson_ProcessesPaymentSuccessfully()
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
        var ticket = new Ticket
        {
            Id = 1,
            Status = TicketStatus.reserved,
            ReservedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        _ticketRepository.GetByIdAsync(1).Returns(ticket);
        _paymentRepository.GetByTicketIdAsync(1).Returns((Payment?)null);
        _paymentRepository.CreateAsync(Arg.Any<Payment>()).Returns(ci => ci.Arg<Payment>());
        _stateService.TransitionToPaidAsync(1, "TXN-001").Returns(true);

        var json = JsonSerializer.Serialize(evt);
        var result = await _sut.HandleAsync(json);

        Assert.True(result.IsSuccess);
        _statusPublisher.Received(1).Publish(1, "paid");
    }

    [Fact]
    public async Task HandleAsync_InvalidJson_ThrowsJsonException()
    {
        await Assert.ThrowsAsync<JsonException>(
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
