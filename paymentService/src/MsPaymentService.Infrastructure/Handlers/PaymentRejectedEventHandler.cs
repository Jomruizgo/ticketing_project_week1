using System.Text.Json;
using Microsoft.Extensions.Options;
using MsPaymentService.Application.Dtos;
using MsPaymentService.Application.Events;
using MsPaymentService.Application.Interfaces;
using MsPaymentService.Application.UseCases.ProcessRejectedPayment;
using MsPaymentService.Infrastructure.Configurations;
using MsPaymentService.Infrastructure.Messaging;

namespace MsPaymentService.Infrastructure.Handlers;

public class PaymentRejectedEventHandler : IPaymentEventHandler
{
    private readonly ProcessRejectedPaymentCommandHandler _commandHandler;
    private readonly IStatusChangedPublisher _statusPublisher;
    private readonly RabbitMQSettings _settings;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PaymentRejectedEventHandler(
        ProcessRejectedPaymentCommandHandler commandHandler,
        IStatusChangedPublisher statusPublisher,
        IOptions<RabbitMQSettings> settings)
    {
        _commandHandler = commandHandler;
        _statusPublisher = statusPublisher;
        _settings = settings.Value;
    }

    public string QueueName => _settings.RejectedQueueName;

    public async Task<ValidationResult> HandleAsync(string json, CancellationToken cancellationToken = default)
    {
        var evt = JsonSerializer.Deserialize<PaymentRejectedEvent>(json, JsonOptions);
        if (evt == null)
            return ValidationResult.Failure("Invalid JSON for PaymentRejectedEvent");

        var command = new ProcessRejectedPaymentCommand(
            evt.TicketId, evt.PaymentId, evt.ProviderReference,
            evt.RejectionReason, evt.RejectedAt, evt.EventId);

        var result = await _commandHandler.HandleAsync(command);

        if (result.IsSuccess)
            _statusPublisher.Publish((int)evt.TicketId, "released");

        return result;
    }
}
