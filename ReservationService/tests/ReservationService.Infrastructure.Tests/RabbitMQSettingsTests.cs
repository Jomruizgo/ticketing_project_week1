using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using ReservationService.Infrastructure.Messaging;

namespace ReservationService.Infrastructure.Tests;

/// <summary>
/// Pruebas de la capa de infraestructura — Mensajería RabbitMQ.
///
/// Validan la configuración y contratos de los componentes de mensajería
/// sin requerir una instancia real de RabbitMQ.
///
/// 7 Principios — Principio 4 (Defect clustering): los bugs de configuración
/// de mensajería se concentran en los valores por defecto y la serialización.
/// 7 Principios — Principio 5 (Pesticide paradox): se diversifican los
/// casos para cubrir configuración, defaults y comportamiento de settings.
/// </summary>
public class RabbitMQSettingsTests
{
    // ─── Valores por defecto ──────────────────────────────────────────────────────

    [Fact(DisplayName = "RabbitMQSettings: valores por defecto son correctos para desarrollo local")]
    public void RabbitMQSettings_DefaultValues_AreCorrectForLocalDev()
    {
        var settings = new RabbitMQSettings();

        Assert.Equal("localhost", settings.Host);
        Assert.Equal(5672, settings.Port);
        Assert.Equal("guest", settings.Username);
        Assert.Equal("guest", settings.Password);
    }

    [Fact(DisplayName = "RabbitMQSettings: nombre de sección es 'RabbitMQ'")]
    public void RabbitMQSettings_SectionName_IsRabbitMQ()
    {
        Assert.Equal("RabbitMQ", RabbitMQSettings.SectionName);
    }

    [Fact(DisplayName = "RabbitMQSettings: queue names por defecto están definidos")]
    public void RabbitMQSettings_DefaultQueueNames_AreDefined()
    {
        var settings = new RabbitMQSettings();

        Assert.False(string.IsNullOrWhiteSpace(settings.QueueName));
        Assert.False(string.IsNullOrWhiteSpace(settings.ExpiredQueueName));
    }

    [Fact(DisplayName = "RabbitMQSettings: exchange y routing keys por defecto están definidos")]
    public void RabbitMQSettings_DefaultExchangeAndRoutingKeys_AreDefined()
    {
        var settings = new RabbitMQSettings();

        Assert.False(string.IsNullOrWhiteSpace(settings.ExchangeName));
        Assert.False(string.IsNullOrWhiteSpace(settings.RoutingKey));
        Assert.False(string.IsNullOrWhiteSpace(settings.StatusChangedRoutingKey));
    }

    // ─── Sobrescritura de valores ──────────────────────────────────────────────────

    [Fact(DisplayName = "RabbitMQSettings: se pueden sobrescribir todos los valores")]
    public void RabbitMQSettings_AllValues_AreOverridable()
    {
        var settings = new RabbitMQSettings
        {
            Host = "rabbitmq-prod",
            Port = 5671,
            Username = "admin",
            Password = "secret",
            QueueName = "q.prod.reserved",
            ExpiredQueueName = "q.prod.expired",
            ExchangeName = "prod-exchange",
            RoutingKey = "prod.ticket.reserved",
            StatusChangedRoutingKey = "prod.ticket.status"
        };

        Assert.Equal("rabbitmq-prod", settings.Host);
        Assert.Equal(5671, settings.Port);
        Assert.Equal("q.prod.reserved", settings.QueueName);
        Assert.Equal("q.prod.expired", settings.ExpiredQueueName);
    }

    // ─── Integración de opciones (IOptions) ──────────────────────────────────────

    [Fact(DisplayName = "RabbitMQSettings: se puede inyectar correctamente via IOptions")]
    public void RabbitMQSettings_CanBeInjectedViaIOptions()
    {
        var settings = new RabbitMQSettings { Host = "test-host", Port = 5672 };
        var options = Options.Create(settings);

        Assert.Equal("test-host", options.Value.Host);
        Assert.Equal(5672, options.Value.Port);
    }

    [Fact(DisplayName = "RabbitMQSettings: QueueName y ExpiredQueueName son distintos")]
    public void RabbitMQSettings_QueueNameAndExpiredQueueName_AreDifferent()
    {
        var settings = new RabbitMQSettings();

        Assert.NotEqual(settings.QueueName, settings.ExpiredQueueName);
    }
}
