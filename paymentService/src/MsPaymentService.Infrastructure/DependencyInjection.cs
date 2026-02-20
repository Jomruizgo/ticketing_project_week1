using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MsPaymentService.Application.Interfaces;
using MsPaymentService.Application.UseCases.ProcessApprovedPayment;
using MsPaymentService.Application.UseCases.ProcessRejectedPayment;
using MsPaymentService.Domain.Entities;
using MsPaymentService.Domain.Interfaces;
using MsPaymentService.Infrastructure.Configurations;
using MsPaymentService.Infrastructure.Handlers;
using MsPaymentService.Infrastructure.Messaging;
using MsPaymentService.Infrastructure.Messaging.RabbitMQ;
using MsPaymentService.Infrastructure.Persistence;
using MsPaymentService.Infrastructure.Persistence.Repositories;
using MsPaymentService.Infrastructure.Services;
using Npgsql;

namespace MsPaymentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("TicketingDb");
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.MapEnum<TicketStatus>("ticket_status");
        dataSourceBuilder.MapEnum<PaymentStatus>("payment_status");
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<PaymentDbContext>(options =>
        {
            options.UseNpgsql(dataSource, npgsqlOptions =>
            {
                npgsqlOptions.CommandTimeout(30);
            });
        });

        // Repositories
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ITicketHistoryRepository, TicketHistoryRepository>();

        // Application use case handlers
        services.AddScoped<ProcessApprovedPaymentCommandHandler>();
        services.AddScoped<ProcessRejectedPaymentCommandHandler>();

        // Application service ports (implemented in Infrastructure)
        services.AddScoped<ITicketStateService, TicketStateService>();

        // RabbitMQ configuration and connection
        services.Configure<RabbitMQSettings>(configuration.GetSection("RabbitMQ"));
        services.Configure<PaymentSettings>(configuration.GetSection("PaymentSettings"));
        services.AddSingleton<RabbitMQConnection>();
        services.AddSingleton<TicketPaymentConsumer>();

        // Status publisher (Singleton — shares the same RabbitMQ connection)
        services.AddSingleton<IStatusChangedPublisher, StatusChangedPublisher>();

        // Messaging handlers (OCP: adding a new event type = registering a new handler)
        services.AddScoped<IPaymentEventHandler, PaymentRequestedEventHandler>();
        services.AddScoped<IPaymentEventHandler, PaymentApprovedEventHandler>();
        services.AddScoped<IPaymentEventHandler, PaymentRejectedEventHandler>();
        services.AddScoped<IPaymentEventDispatcher, PaymentEventDispatcherImpl>();

        return services;
    }
}
