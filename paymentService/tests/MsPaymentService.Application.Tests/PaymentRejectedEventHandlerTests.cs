using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MsPaymentService.Application.Events;
using MsPaymentService.Application.Interfaces;
using MsPaymentService.Application.UseCases.ProcessRejectedPayment;
using MsPaymentService.Domain.Entities;
using MsPaymentService.Domain.Interfaces;
using MsPaymentService.Infrastructure.Configurations;
using MsPaymentService.Infrastructure.Handlers;
using MsPaymentService.Infrastructure.Messaging;
using NSubstitute;
using Xunit;

namespace MsPaymentService.Application.Tests;

public class PaymentRejectedEventHandlerTests
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketStateService _stateService;
    private readonly IStatusChangedPublisher _statusPublisher;
    private readonly PaymentRejectedEventHandler _sut;

    public PaymentRejectedEventHandlerTests()
    {
        _ticketRepository = Substitute.For<ITicketRepository>();
        _stateService = Substitute.For<ITicketStateService>();
        _statusPublisher = Substitute.For<IStatusChangedPublisher>();

        var commandHandler = new ProcessRejectedPaymentCommandHandler(
            _ticketRepository,
            _stateService,
            Substitute.For<ILogger<ProcessRejectedPaymentCommandHandler>>());

        var settings = Options.Create(new RabbitMQSettings
        {
            ApprovedQueueName = "ticket.payments.approved",
            RejectedQueueName = "ticket.payments.rejected"
        });

        _sut = new PaymentRejectedEventHandler(commandHandler, _statusPublisher, settings);
    }

    [Fact]
    public void QueueName_ReturnsRejectedQueueName()
    {
        Assert.Equal("ticket.payments.rejected", _sut.QueueName);
    }

    [Fact]
    public async Task HandleAsync_ValidJson_ProcessesRejectionSuccessfully()
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
        _ticketRepository.GetByIdAsync(1).Returns(new Ticket { Id = 1, Status = TicketStatus.reserved });
        _stateService.TransitionToReleasedAsync(1, Arg.Any<string>()).Returns(true);

        var json = JsonSerializer.Serialize(evt);
        var result = await _sut.HandleAsync(json);

        Assert.True(result.IsSuccess);
        _statusPublisher.Received(1).Publish(1, "released");
    }

    [Fact]
    public async Task HandleAsync_NullDeserialization_ReturnsFailure()
    {
        var result = await _sut.HandleAsync("null");

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid JSON", result.FailureReason!);
    }
}
