using CrudService.Data;
using CrudService.Extensions;
using CrudService.Messaging;
using CrudService.Models.Entities;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

NpgsqlConnection.GlobalTypeMapper.MapEnum<TicketStatus>("ticket_status");
NpgsqlConnection.GlobalTypeMapper.MapEnum<PaymentStatus>("payment_status");

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
