using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReservationService.Application.UseCases.ProcessReservation;
using ReservationService.Domain.Interfaces;
using ReservationService.Infrastructure.Messaging;
using ReservationService.Infrastructure.Persistence;
using ReservationService.Infrastructure.Persistence.Repositories;

namespace ReservationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // RabbitMQ settings
        services.Configure<RabbitMQSettings>(
            configuration.GetSection(RabbitMQSettings.SectionName));

        // Database
        services.AddDbContext<TicketingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories (adapters implementing domain ports)
        services.AddScoped<ITicketRepository, TicketRepository>();

        // Application use cases
        services.AddScoped<ProcessReservationCommandHandler>();

        // Messaging consumer
        services.AddHostedService<RabbitMQConsumer>();

        return services;
    }
}
