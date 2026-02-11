using Microsoft.EntityFrameworkCore;
using MsPaymentService.Worker.Data;
using MsPaymentService.Worker.Models.Entities;
using Npgsql;

namespace MsPaymentService.Worker.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TicketingDb");

        // Registrar enums de PostgreSQL en Npgsql
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
            
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });
        
        return services;
    }
}