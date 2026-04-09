using CrudService.Application.Services;
using CrudService.Infrastructure.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace CrudService.Api.Tests;

/// <summary>
/// Factory de pruebas de Caja Negra.
/// Levanta el servidor HTTP real de CrudService pero reemplaza la infraestructura
/// (base de datos, RabbitMQ) por dobles de prueba (NSubstitute mocks).
/// El test NO conoce la implementación interna — solo interactúa vía HTTP.
/// </summary>
public class CrudServiceApiFactory : WebApplicationFactory<Program>
{
    public ITicketService TicketServiceMock { get; } = Substitute.For<ITicketService>();
    public IEventService EventServiceMock { get; } = Substitute.For<IEventService>();

    // Set environment variables BEFORE Program.cs runs.
    // Program.cs calls AddEnvironmentVariables() which overrides
    // the placeholder literals in appsettings.json.
    static CrudServiceApiFactory()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Host=localhost;Port=5432;Database=test;Username=test;Password=test");
        Environment.SetEnvironmentVariable("RabbitMQ__Host", "localhost");
        Environment.SetEnvironmentVariable("RabbitMQ__Username", "guest");
        Environment.SetEnvironmentVariable("RabbitMQ__Password", "guest");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Eliminar servicios de infraestructura real (BD, RabbitMQ)
            services.RemoveAll<ITicketService>();
            services.RemoveAll<IEventService>();

            // Quitar todos los hosted services (consumers RabbitMQ)
            var hostedDescriptors = services
                .Where(d => d.ImplementationType != null &&
                            d.ImplementationType.Name.Contains("Consumer"))
                .ToList();
            foreach (var descriptor in hostedDescriptors)
                services.Remove(descriptor);

            // Registrar mocks en su lugar
            services.AddSingleton(TicketServiceMock);
            services.AddSingleton(EventServiceMock);
        });
    }
}
