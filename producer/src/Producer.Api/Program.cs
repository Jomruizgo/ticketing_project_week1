using Producer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddProducerInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // ️ HUMAN CHECK:
        // La IA sugirió AllowAnyOrigin() como "patrón por defecto"
        // Lo mantuvimos SOLO para el MVP/desarrollo local.
        // En producción: DEBE ser específico:
        // policy.WithOrigins("https://app.example.com")
        //       .WithMethods("GET", "POST", "PATCH")
        //       .WithHeaders("Content-Type", "Authorization")
        //       .AllowCredentials();
        // AllowAnyOrigin() + AllowAnyMethod() abre vulnerabilidades CSRF
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health")
    .WithOpenApi()
    .Produces(StatusCodes.Status200OK);

app.Run();
