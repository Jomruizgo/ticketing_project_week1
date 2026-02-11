using System.Text.Json;
using Microsoft.Extensions.Options;
using MsPaymentService.Worker.Configurations;
using MsPaymentService.Worker.Models.DTOs;
using MsPaymentService.Worker.Models.Events;
using MsPaymentService.Worker.Services;

namespace MsPaymentService.Worker.Handlers;

/// <summary>
/// Handler para eventos de pago aprobado. Única responsabilidad: deserializar y delegar en el servicio de validación.
/// </summary>
public class PaymentApprovedEventHandler : IPaymentEventHandler
{
    private readonly IPaymentValidationService _validationService;
    private readonly RabbitMQSettings _settings;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PaymentApprovedEventHandler(
        IPaymentValidationService validationService,
        IOptions<RabbitMQSettings> settings)
    {
        _validationService = validationService;
        _settings = settings.Value;
    }

    public string QueueName => _settings.ApprovedQueueName;

    public async Task<ValidationResult> HandleAsync(string json, CancellationToken cancellationToken = default)
    {
        var evt = JsonSerializer.Deserialize<PaymentApprovedEvent>(json, JsonOptions);
        if (evt == null)
            return ValidationResult.Failure("Invalid JSON for PaymentApprovedEvent");

        return await _validationService.ValidateAndProcessApprovedPaymentAsync(evt);
    }
}
