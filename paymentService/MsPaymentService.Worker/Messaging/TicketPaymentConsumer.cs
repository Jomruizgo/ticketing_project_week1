using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using MsPaymentService.Worker.Configurations;
using MsPaymentService.Worker.Models.Events;
using MsPaymentService.Worker.Models.DTOs;
using MsPaymentService.Worker.Services;

namespace MsPaymentService.Worker.Messaging.RabbitMQ;

public class TicketPaymentConsumer
{
    private readonly RabbitMQConnection _connection;
    private readonly RabbitMQSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TicketPaymentConsumer> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TicketPaymentConsumer(
        RabbitMQConnection connection,
        IOptions<RabbitMQSettings> settings,
        IServiceScopeFactory scopeFactory,
        ILogger<TicketPaymentConsumer> logger)
    {
        _connection = connection;
        _settings = settings.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Start(string queueName)
    {
        var channel = _connection.GetChannel();

        channel.BasicQos(0, _settings.PrefetchCount, false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += OnMessageReceivedAsync;

        channel.BasicConsume(
            queue: queueName,
            autoAck: false,
            consumer: consumer
        );

        _logger.LogInformation(
            "TicketPaymentConsumer escuchando cola {Queue}",
            queueName
        );
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        var channel = _connection.GetChannel();

        _logger.LogInformation(
            "[Consumer] Mensaje recibido. RoutingKey: {RoutingKey}, DeliveryTag: {DeliveryTag}, Exchange: {Exchange}",
            args.RoutingKey, args.DeliveryTag, args.Exchange);

        try
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());
            _logger.LogInformation("[Consumer] Payload recibido: {Json}", json);

            using var scope = _scopeFactory.CreateScope();
            var validationService = scope.ServiceProvider
                .GetRequiredService<IPaymentValidationService>();

            if (args.RoutingKey == "ticket.payments.approved")
            {
                _logger.LogInformation("[Consumer] Procesando evento APPROVED...");
                var evt = JsonSerializer.Deserialize<PaymentApprovedEvent>(json, _jsonOptions);
                _logger.LogInformation("[Consumer] Evento deserializado: TicketId={TicketId}, EventId={EventId}, OrderId={OrderId}",
                    evt?.TicketId, evt?.EventId, evt?.OrderId);
                var result = await validationService.ValidateAndProcessApprovedPaymentAsync(evt);
                _logger.LogInformation("[Consumer] Resultado validación: IsSuccess={IsSuccess}, FailureReason={FailureReason}",
                    result.IsSuccess, result.FailureReason);
                HandleResult(result, channel, args);
            }
            else if (args.RoutingKey == "ticket.payments.rejected")
            {
                _logger.LogInformation("[Consumer] Procesando evento REJECTED...");
                var evt = JsonSerializer.Deserialize<PaymentRejectedEvent>(json, _jsonOptions);
                _logger.LogInformation("[Consumer] Evento deserializado: TicketId={TicketId}", evt?.TicketId);
                var result = await validationService.ValidateAndProcessRejectedPaymentAsync(evt);
                _logger.LogInformation("[Consumer] Resultado validación: IsSuccess={IsSuccess}, FailureReason={FailureReason}",
                    result.IsSuccess, result.FailureReason);
                HandleResult(result, channel, args);
            }
            else
            {
                _logger.LogWarning(
                    "[Consumer] Evento con routing key desconocida: {RoutingKey}",
                    args.RoutingKey);
                channel.BasicAck(args.DeliveryTag, false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Consumer] Error procesando evento. RoutingKey: {RoutingKey}, DeliveryTag: {DeliveryTag}",
                args.RoutingKey, args.DeliveryTag);

            channel.BasicNack(
                deliveryTag: args.DeliveryTag,
                multiple: false,
                requeue: false // DLQ
            );
        }
    }

    private void HandleResult(
        ValidationResult result,
        IModel channel,
        BasicDeliverEventArgs args)
    {
        if (result.IsSuccess || result.IsAlreadyProcessed)
        {
            channel.BasicAck(args.DeliveryTag, false);
            return;
        }

        // Error de negocio → NO requeue
        if (!string.IsNullOrEmpty(result.FailureReason))
        {
            channel.BasicAck(args.DeliveryTag, false);
            return;
        }

        // Error técnico → DLQ
        channel.BasicNack(
            deliveryTag: args.DeliveryTag,
            multiple: false,
            requeue: false
        );
    }

}
