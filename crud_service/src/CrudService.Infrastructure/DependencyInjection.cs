using CrudService.Infrastructure.Persistence;
using CrudService.Infrastructure.Persistence.Repositories;
using CrudService.Infrastructure.Messaging;
using CrudService.Infrastructure.Strategies;
using CrudService.Infrastructure.Services;
using CrudService.Domain.Interfaces;
using CrudService.Application.Services;
using CrudService.Application.UseCases.Waitlist.EnrollInWaitlist;
using CrudService.Application.UseCases.Waitlist.GetWaitlistStatus;
using CrudService.Application.UseCases.Waitlist.AssignOpportunity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CrudService.Infrastructure;

/// <summary>
/// Extensiones para registrar servicios de la aplicación (DI).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra todos los servicios, repositorios e interfaces ISP.
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext (Scoped: una conexión por request HTTP)
        services.AddDbContext<TicketingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositorios (Scoped: viven en ciclo del request)
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ITicketHistoryRepository, TicketHistoryRepository>();
        services.AddScoped<IWaitlistEntryRepository, WaitlistEntryRepository>();
        services.AddScoped<IWaitlistOpportunityRepository, WaitlistOpportunityRepository>();

        // Servicios (Scoped: dependen de repositorios)
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IEnrollInWaitlistUseCase, EnrollInWaitlistHandler>();
        services.AddScoped<IGetWaitlistStatusUseCase, GetWaitlistStatusHandler>();

        // Waitlist opportunity assignment
        services.AddScoped<IPrioritizationStrategy, FifoStrategy>();
        services.AddScoped<ITicketReservationPort, TicketReservationAdapter>();
        services.AddScoped<IOpportunityObserver, OpportunityActivatedObserver>();
        services.AddScoped<IAssignOpportunityUseCase, AssignOpportunityHandler>();

        // SSE hub (Singleton: correlaciona ticketId con conexiones activas)
        services.AddSingleton<TicketStatusHub>();

        // ISP: cada consumidor recibe solo la interfaz que necesita (DIP)
        // Consumer → ITicketStatusNotifier (solo Notify)
        // Controller → ITicketStatusSubscriber (solo Subscribe)
        services.AddSingleton<ITicketStatusNotifier>(sp =>
            sp.GetRequiredService<TicketStatusHub>());
        services.AddSingleton<ITicketStatusSubscriber>(sp =>
            sp.GetRequiredService<TicketStatusHub>());

        return services;
    }
}
