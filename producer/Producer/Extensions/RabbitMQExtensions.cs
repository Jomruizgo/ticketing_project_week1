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
            var options = provider.GetRequiredService<IOptions<RabbitMQOptions>>().Value;
            var factory = new ConnectionFactory
            {
                HostName = options.Host,
                Port = options.Port,
                UserName = options.Username,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            return factory.CreateConnection();
        });

        // Registrar el publicador
        services.AddScoped<ITicketPublisher, RabbitMQTicketPublisher>();

        return services;
    }
}
