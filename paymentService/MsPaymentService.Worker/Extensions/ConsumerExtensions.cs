using Microsoft.Extensions.DependencyInjection;
using MsPaymentService.Worker.Messaging.RabbitMQ;
using MsPaymentService.Worker.Configurations;

namespace MsPaymentService.Worker.Extensions;

public static class ConsumerExtensions
{
    public static IServiceCollection AddTicketPaymentConsumer(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Configuración
        services.Configure<RabbitMQSettings>(configuration.GetSection("RabbitMQ"));
        services.Configure<PaymentSettings>(configuration.GetSection("PaymentSettings"));
        
        // Servicios de RabbitMQ
        services.AddSingleton<RabbitMQConnection>();
        services.AddSingleton<TicketPaymentConsumer>();
        return services;
    }
}
