using CrudService.Application.UseCases.Events.CreateEvent;
using CrudService.Application.UseCases.Events.DeleteEvent;
using CrudService.Application.UseCases.Events.GetAllEvents;
using CrudService.Application.UseCases.Events.GetEventById;
using CrudService.Application.UseCases.Events.UpdateEvent;
using CrudService.Application.UseCases.Tickets.CreateTickets;
using CrudService.Application.UseCases.Tickets.GetExpiredTickets;
using CrudService.Application.UseCases.Tickets.GetTicketById;
using CrudService.Application.UseCases.Tickets.GetTicketsByEvent;
using CrudService.Application.UseCases.Tickets.ReleaseTicket;
using CrudService.Application.UseCases.Tickets.UpdateTicketStatus;
using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;
using CrudService.Infrastructure.Messaging;
using CrudService.Infrastructure.Persistence;
using CrudService.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CrudService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Registro de enums de PostgreSQL (debe ejecutarse antes de abrir conexiones)
        NpgsqlConnection.GlobalTypeMapper.MapEnum<TicketStatus>("ticket_status");
        NpgsqlConnection.GlobalTypeMapper.MapEnum<PaymentStatus>("payment_status");

        // DbContext (Scoped: nueva instancia por request)
        services.AddDbContext<TicketingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositorios (Scoped: dependen de DbContext)
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ITicketHistoryRepository, TicketHistoryRepository>();

        // Use Case Handlers — Events (Scoped: dependen de repositorios)
        services.AddScoped<GetAllEventsQueryHandler>();
        services.AddScoped<GetEventByIdQueryHandler>();
        services.AddScoped<CreateEventCommandHandler>();
        services.AddScoped<UpdateEventCommandHandler>();
        services.AddScoped<DeleteEventCommandHandler>();

        // Use Case Handlers — Tickets (Scoped)
        services.AddScoped<GetTicketsByEventQueryHandler>();
        services.AddScoped<GetTicketByIdQueryHandler>();
        services.AddScoped<CreateTicketsCommandHandler>();
        services.AddScoped<UpdateTicketStatusCommandHandler>();
        services.AddScoped<ReleaseTicketCommandHandler>();
        services.AddScoped<GetExpiredTicketsQueryHandler>();

        // SSE hub (Singleton: correlaciona ticketId con conexiones SSE activas)
        services.AddSingleton<TicketStatusHub>();

        return services;
    }
}
