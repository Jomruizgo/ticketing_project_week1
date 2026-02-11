using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReservationService.Worker.Configurations;
using ReservationService.Worker.Models;
using ReservationService.Worker.Services;

namespace ReservationService.Worker.Consumers;

// BackgroundService = servicio que corre en segundo plano mientras la app está activa
public class TicketReservationConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<TicketReservationConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public TicketReservationConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMQSettings> settings,
        ILogger<TicketReservationConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    // Este método se ejecuta cuando inicia la aplicación
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 1. CONEXIÓN a RabbitMQ
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.Username,
            Password = _settings.Password
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        _logger.LogInformation("Connected to RabbitMQ. Listening on queue: {Queue}", _settings.QueueName);

        // 2. SUSCRIPCIÓN - crear el consumer y definir qué hacer cuando llega un mensaje
        var consumer = new AsyncEventingBasicConsumer(_channel);

        // 3. CALLBACK - esto se ejecuta cada vez que llega un mensaje
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            // Leer el mensaje (viene como bytes, lo convertimos a string)
            var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            _logger.LogInformation("Message received: {Json}", json);

            try
            {
                // Deserializar JSON a nuestro objeto ReservationMessage
                var message = JsonSerializer.Deserialize<ReservationMessage>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (message is not null)
                {
                    // Crear un scope para obtener los servicios (DbContext es scoped)
                    using var scope = _scopeFactory.CreateScope();
                    var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();

                    // Procesar la reserva
                    await reservationService.ProcessReservationAsync(message, stoppingToken);
                }

                // ACK - decirle a RabbitMQ que ya procesamos el mensaje
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Json}", json);
                // Aún con error, hacemos ACK para no reintentar infinitamente (MVP simple)
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, stoppingToken);
            }
        };

        // Empezar a escuchar la cola (autoAck: false = nosotros controlamos el ACK)
        await _channel.BasicConsumeAsync(_settings.QueueName, autoAck: false, consumer, stoppingToken);

        // Mantener el servicio corriendo hasta que se detenga la app
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    // Se ejecuta cuando la app se cierra - limpiamos conexiones
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping consumer...");

        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }
}
