using System.Text.Json;
using Microsoft.Extensions.Options;
using MsPaymentService.Worker.Configurations;
using MsPaymentService.Worker.Messaging;
using MsPaymentService.Worker.Models.DTOs;
using MsPaymentService.Worker.Models.Events;
using MsPaymentService.Worker.Services;

namespace MsPaymentService.Worker.Handlers;

/// <summary>
/// Handler para eventos de pago aprobado. Deserializa, delega en el servicio de validación,
/// y publica ticket.status.changed tras actualizar la DB.
/// </summary>
public class PaymentApprovedEventHandler : IPaymentEventHandler
{
    private readonly IPaymentValidationService _validationService;
    private readonly IStatusChangedPublisher _statusPublisher;
    private readonly RabbitMQSettings _settings;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PaymentApprovedEventHandler(
        IPaymentValidationService validationService,
        IStatusChangedPublisher statusPublisher,
        IOptions<RabbitMQSettings> settings)
    {
        _validationService = validationService;
        _statusPublisher = statusPublisher;
        _settings = settings.Value;
    }

    public string QueueName => _settings.ApprovedQueueName;

    public async Task<ValidationResult> HandleAsync(string json, CancellationToken cancellationToken = default)
    {
        var evt = JsonSerializer.Deserialize<PaymentApprovedEvent>(json, JsonOptions);
        if (evt == null)
            return ValidationResult.Failure("Invalid JSON for PaymentApprovedEvent");

        var result = await _validationService.ValidateAndProcessApprovedPaymentAsync(evt);

        if (result.IsSuccess)
            _statusPublisher.Publish((int)evt.TicketId, "paid");

        return result;
    }
}
