using Microsoft.EntityFrameworkCore;
using ReservationService.Worker.Configurations;
using ReservationService.Worker.Consumers;
using ReservationService.Worker.Data;
using ReservationService.Worker.Repositories;
using ReservationService.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

// 1. CONFIGURACIÓN - Leer RabbitMQ settings desde appsettings.json
builder.Services.Configure<RabbitMQSettings>(
    builder.Configuration.GetSection(RabbitMQSettings.SectionName));

// 2. BASE DE DATOS - Registrar DbContext con PostgreSQL
builder.Services.AddDbContext<TicketingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. SERVICIOS - Registrar nuestras clases
// "Scoped" = se crea una instancia nueva por cada solicitud/scope
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IReservationService, ReservationServiceImpl>();

// 4. CONSUMER - Registrar como servicio en segundo plano
// "HostedService" = se inicia automáticamente cuando arranca la app
builder.Services.AddHostedService<TicketReservationConsumer>();

var host = builder.Build();
host.Run();
