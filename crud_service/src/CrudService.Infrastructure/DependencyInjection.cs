using CrudService.Infrastructure.Sse;
using CrudService.Infrastructure.Persistence;
using CrudService.Infrastructure.Persistence.Repositories;
using CrudService.Infrastructure.Messaging;
using CrudService.Infrastructure.Strategies;
using CrudService.Infrastructure.Services;
using CrudService.Domain.Entities;
using CrudService.Domain.Enums;
using CrudService.Domain.Interfaces;
using CrudService.Application.Interfaces;
using CrudService.Application.Services;
using CrudService.Application.UseCases.Waitlist.EnrollInWaitlist;
using CrudService.Application.UseCases.Waitlist.GetWaitlistStatus;
using CrudService.Application.UseCases.Waitlist.AssignOpportunity;
using CrudService.Application.UseCases.Waitlist.ExpireOpportunity;
using CrudService.Application.UseCases.Waitlist.ClaimOpportunity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Npgsql.NameTranslation;

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
        // NpgsqlDataSource with enum mappings (snake_case name translator
        // replaces the [PgName] attributes that were previously in Domain)
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        var snakeCaseTranslator = new NpgsqlSnakeCaseNameTranslator();
        dataSourceBuilder.MapEnum<TicketStatus>(nameTranslator: snakeCaseTranslator);
        dataSourceBuilder.MapEnum<PaymentStatus>(nameTranslator: snakeCaseTranslator);
        dataSourceBuilder.MapEnum<WaitlistEntryStatus>(nameTranslator: snakeCaseTranslator);
        dataSourceBuilder.MapEnum<WaitlistOpportunityStatus>(nameTranslator: snakeCaseTranslator);
        dataSourceBuilder.MapEnum<NotificationDeliveryStatus>(nameTranslator: snakeCaseTranslator);
        var dataSource = dataSourceBuilder.Build();

        // DbContext (Scoped: una conexión por request HTTP)
        services.AddDbContext<TicketingDbContext>(options =>
            options.UseNpgsql(dataSource));

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
        services.AddScoped<IOpportunityObserver, EmailNotificationObserver>();
        services.AddScoped<IAssignOpportunityUseCase, AssignOpportunityHandler>();

        // Waitlist opportunity expiration
        services.AddScoped<IInventoryReturnPort, InventoryReturnAdapter>();
        services.AddScoped<IExpireOpportunityUseCase, ExpireOpportunityHandler>();
        services.AddHostedService<WaitlistOpportunityExpiredConsumer>();

        // Waitlist opportunity claim
        services.AddScoped<IClaimOpportunityUseCase, ClaimOpportunityHandler>();

        // Email notification ports
        services.AddScoped<IEmailSender, LogEmailSender>();
        services.AddScoped<INotificationDeliveryRepository, NotificationDeliveryRepository>();

        // SSE hub (Singleton: correlaciona ticketId con conexiones activas)
        services.AddSingleton<TicketStatusHub>();

        // ISP: cada consumidor recibe solo la interfaz que necesita (DIP)
        // Consumer → ITicketStatusNotifier (solo Notify)
        // Controller → ITicketStatusSubscriber (solo Subscribe)
        services.AddSingleton<ITicketStatusNotifier>(sp =>
            sp.GetRequiredService<TicketStatusHub>());
        services.AddSingleton<ITicketStatusSubscriber>(sp =>
            sp.GetRequiredService<TicketStatusHub>());

        // Waitlist SSE hub (Singleton: correlaciona email con conexiones SSE activas)
        services.AddSingleton<WaitlistSseHub>();

        // ISP: consumer → IWaitlistSseNotifier (solo SendEvent)
        //       controller → IWaitlistSseSubscriber (solo Register/Unregister)
        services.AddSingleton<IWaitlistSseNotifier>(sp =>
            sp.GetRequiredService<WaitlistSseHub>());
        services.AddSingleton<IWaitlistSseSubscriber>(sp =>
            sp.GetRequiredService<WaitlistSseHub>());

        return services;
    }
}
