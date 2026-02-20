using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MsPaymentService.Application.Dtos;
using MsPaymentService.Infrastructure.Configurations;
using MsPaymentService.Infrastructure.Handlers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MsPaymentService.Infrastructure.Messaging.RabbitMQ;

public class TicketPaymentConsumer
{
    private readonly RabbitMQConnection _connection;
    private readonly RabbitMQSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TicketPaymentConsumer> _logger;

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

        channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

        _logger.LogInformation("TicketPaymentConsumer escuchando cola {Queue}", queueName);
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
            _logger.LogDebug("[Consumer] Payload: {Json}", json);

            using var scope = _scopeFactory.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IPaymentEventDispatcher>();

            var result = await dispatcher.DispatchAsync(args.RoutingKey, json);

            if (result == null)
            {
                _logger.LogWarning(
                    "[Consumer] No hay handler para la routing key: {RoutingKey}. ACK sin procesar.",
                    args.RoutingKey);
                channel.BasicAck(args.DeliveryTag, false);
                return;
            }

            _logger.LogInformation(
                "[Consumer] Resultado: IsSuccess={IsSuccess}, FailureReason={FailureReason}",
                result.IsSuccess, result.FailureReason);

            HandleResult(result, channel, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Consumer] Error procesando evento. RoutingKey: {RoutingKey}, DeliveryTag: {DeliveryTag}",
                args.RoutingKey, args.DeliveryTag);

            channel.BasicNack(deliveryTag: args.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private static void HandleResult(ValidationResult result, IModel channel, BasicDeliverEventArgs args)
    {
        if (result.IsSuccess || result.IsAlreadyProcessed)
        {
            channel.BasicAck(args.DeliveryTag, false);
            return;
        }

        if (!string.IsNullOrEmpty(result.FailureReason))
        {
            channel.BasicAck(args.DeliveryTag, false);
            return;
        }

        channel.BasicNack(deliveryTag: args.DeliveryTag, multiple: false, requeue: false);
    }
}
