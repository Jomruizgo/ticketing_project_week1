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
        // ️ HUMAN CHECK:
        // La IA sugirió crear DbContext como Transient (nueva instancia por request)eso era demadiado ineficiente porque iba a hacer una satiracion de conexiones en la base, asi que lo cambiamos a Scoped.
        services.AddDbContext<TicketingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositorios (Scoped: viven en ciclo del request)
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ITicketHistoryRepository, TicketHistoryRepository>();

        // Servicios (Scoped: dependen de repositorios)
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<ITicketService, TicketService>();

        return services;
    }
}
