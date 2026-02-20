using System.Text.Json;
using Microsoft.Extensions.Options;
using MsPaymentService.Application.Dtos;
using MsPaymentService.Application.Events;
using MsPaymentService.Application.UseCases.ProcessApprovedPayment;
using MsPaymentService.Infrastructure.Configurations;
using MsPaymentService.Infrastructure.Messaging;

namespace MsPaymentService.Infrastructure.Handlers;

public class PaymentApprovedEventHandler : IPaymentEventHandler
{
    private readonly ProcessApprovedPaymentCommandHandler _commandHandler;
    private readonly IStatusChangedPublisher _statusPublisher;
    private readonly RabbitMQSettings _settings;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PaymentApprovedEventHandler(
        ProcessApprovedPaymentCommandHandler commandHandler,
        IStatusChangedPublisher statusPublisher,
        IOptions<RabbitMQSettings> settings)
    {
        _commandHandler = commandHandler;
        _statusPublisher = statusPublisher;
        _settings = settings.Value;
    }

    public string QueueName => _settings.ApprovedQueueName;

    public async Task<ValidationResult> HandleAsync(string json, CancellationToken cancellationToken = default)
    {
        var evt = JsonSerializer.Deserialize<PaymentApprovedEvent>(json, JsonOptions);
        if (evt == null)
            return ValidationResult.Failure("Invalid JSON for PaymentApprovedEvent");

        var command = new ProcessApprovedPaymentCommand(
            evt.TicketId, evt.EventId, evt.AmountCents,
            evt.Currency, evt.PaymentBy, evt.TransactionRef, evt.ApprovedAt);

        var result = await _commandHandler.HandleAsync(command);

        if (result.IsSuccess)
            _statusPublisher.Publish((int)evt.TicketId, "paid");

        return result;
    }
}
