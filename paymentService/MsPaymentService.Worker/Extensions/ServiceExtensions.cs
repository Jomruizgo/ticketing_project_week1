using MsPaymentService.Worker.Repositories;
using MsPaymentService.Worker.Services;

namespace MsPaymentService.Worker.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Services
        services.AddScoped<IPaymentValidationService, PaymentValidationService>();
        services.AddScoped<ITicketStateService, TicketStateService>();
        
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