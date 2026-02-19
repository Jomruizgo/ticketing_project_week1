using MsPaymentService.Worker.Handlers;
using MsPaymentService.Worker.Messaging;
using MsPaymentService.Worker.Repositories;
using MsPaymentService.Worker.Services;

namespace MsPaymentService.Worker.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Publisher de notificaciones (singleton — usa la misma conexión RabbitMQ)
        services.AddSingleton<IStatusChangedPublisher, StatusChangedPublisher>();

        // Services
        services.AddScoped<IPaymentValidationService, PaymentValidationService>();
        services.AddScoped<ITicketStateService, TicketStateService>();

        // Handlers (OCP: añadir tipo de evento = registrar nuevo handler)
        services.AddScoped<IPaymentEventHandler, PaymentRequestedEventHandler>();
        services.AddScoped<IPaymentEventHandler, PaymentApprovedEventHandler>();
        services.AddScoped<IPaymentEventHandler, PaymentRejectedEventHandler>();
        services.AddScoped<IPaymentEventDispatcher, PaymentEventDispatcherImpl>();

        return services;
    }
    
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ITicketHistoryRepository, TicketHistoryRepository>();
        
        return services;
    }
}