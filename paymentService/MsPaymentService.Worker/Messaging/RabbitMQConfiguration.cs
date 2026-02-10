using Microsoft.Extensions.Options;
using MsPaymentService.Worker.Configurations;
using RabbitMQ.Client;

namespace MsPaymentService.Worker.Messaging.RabbitMQ;

public static class RabbitMQConfiguration
{
    public static void SetupQueuesAndExchanges(IModel channel, RabbitMQSettings settings)
    {
        // Configurar exchange principal
        channel.ExchangeDeclare(
            exchange: "ticket.payments",
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        // Configurar Dead Letter Exchange
        channel.ExchangeDeclare(
            exchange: "ticket.payments.dlx",
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        // Cola para pagos aprobados
        SetupQueue(channel, settings.PaymentApprovedQueue, "ticket.payments.dlx", "ticket.payments.dlq");
        
        // Cola para pagos rechazados
        SetupQueue(channel, settings.PaymentRejectedQueue, "ticket.payments.dlx", "ticket.payments.dlq");

        // Dead Letter Queue
        channel.QueueDeclare(
            queue: "ticket.payments.dlq",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueBind(
            queue: "ticket.payments.dlq",
            exchange: "ticket.payments.dlx",
            routingKey: "ticket.payments.dlq");
    }

    private static void SetupQueue(IModel channel, QueueSettings queueSettings, string dlxExchange, string dlqRoutingKey)
    {
        var arguments = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", dlxExchange },
            { "x-dead-letter-routing-key", dlqRoutingKey }
        };

        channel.QueueDeclare(
            queue: queueSettings.QueueName,
            durable: queueSettings.Durable,
            exclusive: false,
            autoDelete: queueSettings.AutoDelete,
            arguments: arguments);

        channel.QueueBind(
            queue: queueSettings.QueueName,
            exchange: queueSettings.ExchangeName,
            routingKey: queueSettings.RoutingKey);
    }
}