using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using MsPaymentService.Worker.Models.Events;
using MsPaymentService.Worker.Models.DTOs;
using MsPaymentService.Worker.Services;

namespace MsPaymentService.Worker.Messaging.RabbitMQ;

public class TicketPaymentConsumer
{
    private readonly RabbitMQConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TicketPaymentConsumer> _logger;

    public TicketPaymentConsumer(
        RabbitMQConnection connection,
        IServiceScopeFactory scopeFactory,
        ILogger<TicketPaymentConsumer> logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Start(string queueName)
    {
        var channel = _connection.GetChannel();

        channel.BasicQos(0, 1, false);

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

        try
        {
            var json = Encoding.UTF8.GetString(args.Body.ToArray());

            using var scope = _scopeFactory.CreateScope();
            var validationService = scope.ServiceProvider
                .GetRequiredService<IPaymentValidationService>();

            if (args.RoutingKey == "ticket.payments.approved")
            {
                var evt = JsonSerializer.Deserialize<PaymentApprovedEvent>(json);
                await validationService.ValidateAndProcessApprovedPaymentAsync(evt);
            }
            else if (args.RoutingKey == "ticket.payments.rejected")
            {
                var evt = JsonSerializer.Deserialize<PaymentRejectedEvent>(json);
                await validationService.ValidateAndProcessRejectedPaymentAsync(evt);
            }
            else
            {
                _logger.LogWarning(
                    "Evento con routing key desconocida: {RoutingKey}",
                    args.RoutingKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando evento {RoutingKey}", args.RoutingKey);

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
