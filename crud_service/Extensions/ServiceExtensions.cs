using CrudService.Data;
using CrudService.Repositories;
using CrudService.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace CrudService.Extensions;

/// <summary>
/// Extensiones para registrar servicios
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Registra todos los servicios y repositorios
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Base de datos
        services.AddDbContext<TicketingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositorios
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ITicketHistoryRepository, TicketHistoryRepository>();

        // Servicios
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<ITicketService, TicketService>();

        return services;
    }
}
