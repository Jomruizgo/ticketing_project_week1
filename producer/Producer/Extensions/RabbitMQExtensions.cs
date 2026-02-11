using Microsoft.Extensions.Options;
using Producer.Configurations;
using Producer.Services;
using RabbitMQ.Client;

namespace Producer.Extensions;

/// <summary>
/// Extensiones para registrar servicios de RabbitMQ
/// </summary>
public static class RabbitMQExtensions
{
    /// <summary>
    /// Registra la conexión de RabbitMQ y los servicios asociados
    /// </summary>
    public static IServiceCollection AddRabbitMQ(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configurar opciones
        services.Configure<RabbitMQOptions>(
            configuration.GetSection(RabbitMQOptions.SectionName));

        // Registrar la conexión de RabbitMQ como singleton
        services.AddSingleton<IConnection>(provider =>
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("RabbitMQ.Configuration");
            var options = provider.GetRequiredService<IOptions<RabbitMQOptions>>().Value;

            logger.LogInformation("Configurando conexión RabbitMQ: Host={Host}, Port={Port}, VirtualHost={VirtualHost}",
                options.Host, options.Port, options.VirtualHost);

            var factory = new ConnectionFactory
            {
                HostName = options.Host,
                Port = options.Port,
                UserName = options.Username,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
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

        // Registrar el publicador de tickets
        services.AddScoped<ITicketPublisher, RabbitMQTicketPublisher>();

        // Registrar el publicador de pagos
        services.AddScoped<IPaymentPublisher, RabbitMQPaymentPublisher>();

        return services;
    }
}
