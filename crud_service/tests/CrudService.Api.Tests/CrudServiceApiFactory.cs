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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Eliminar servicios de infraestructura real (BD, RabbitMQ)
            services.RemoveAll<ITicketService>();
            services.RemoveAll<IEventService>();

            // Quitar el hosted service de RabbitMQ para no intentar conectarse
            var descriptor = services.FirstOrDefault(d =>
                d.ImplementationType == typeof(TicketStatusConsumer));
            if (descriptor is not null)
                services.Remove(descriptor);

            // Registrar mocks en su lugar
            services.AddSingleton(TicketServiceMock);
            services.AddSingleton(EventServiceMock);
        });
    }
}
