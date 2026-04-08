using CrudService.Infrastructure.Persistence;
using CrudService.Infrastructure;
using CrudService.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Cargar variables de entorno
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar servicios de aplicación (DbContext, Repositories, Services, TicketStatusHub)
builder.Services.AddApplicationServices(builder.Configuration);

// RabbitMQ consumer para ticket.status.changed
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection(RabbitMQSettings.SectionName));
builder.Services.AddHostedService<TicketStatusConsumer>();
builder.Services.AddHostedService<TicketReleasedConsumer>();
builder.Services.AddHostedService<SseNotificationConsumer>();

// CORS (si es necesario para frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configurar pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health")
    .WithOpenApi()
    .Produces(StatusCodes.Status200OK);

app.Run();

// Expone la clase Program para WebApplicationFactory en tests de Caja Negra
public partial class Program { }
