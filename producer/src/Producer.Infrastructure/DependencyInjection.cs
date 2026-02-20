using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Producer.Application.UseCases.RequestPayment;
using Producer.Application.UseCases.ReserveTicket;
using Producer.Domain.Ports;
using Producer.Infrastructure.Messaging;
using RabbitMQ.Client;

namespace Producer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddProducerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMQSettings>(
            configuration.GetSection(RabbitMQSettings.SectionName));

        services.AddSingleton<IConnection>(provider =>
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("RabbitMQ.Configuration");
            var settings = provider.GetRequiredService<IOptions<RabbitMQSettings>>().Value;

            logger.LogInformation("Configurando conexión RabbitMQ: Host={Host}, Port={Port}, VirtualHost={VirtualHost}",
                settings.Host, settings.Port, settings.VirtualHost);

            var factory = new ConnectionFactory
            {
                HostName = settings.Host,
                Port = settings.Port,
                // HUMAN CHECK:
                // Credenciales y host se consumen desde configuración externa
                // (appsettings + env vars). No hardcodear secretos en código.
                UserName = settings.Username,
                Password = settings.Password,
                VirtualHost = settings.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(30),
                RequestedHeartbeat = TimeSpan.FromSeconds(10)
            };

            try
            {
                var connection = factory.CreateConnection();
                logger.LogInformation("Conexión RabbitMQ establecida exitosamente");
                return connection;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al conectar con RabbitMQ");
                throw;
            }
        });

        services.AddScoped<ITicketEventPublisher, RabbitMQTicketPublisher>();
        services.AddScoped<IPaymentEventPublisher, RabbitMQPaymentPublisher>();

        services.AddScoped<ReserveTicketCommandHandler>();
        services.AddScoped<RequestPaymentCommandHandler>();

        return services;
    }
}
