using Microsoft.Extensions.Options;
using MsPaymentService.Worker.Configurations;
using RabbitMQ.Client;

namespace MsPaymentService.Worker.Messaging.RabbitMQ;

public class RabbitMQConnection : IDisposable
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQConnection> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private bool _disposed = false;

    public RabbitMQConnection(IOptions<RabbitMQSettings> settings, ILogger<RabbitMQConnection> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public IConnection GetConnection()
    {
        if (_connection == null || !_connection.IsOpen)
        {
            CreateConnection();
        }
        return _connection!;
    }

    public IModel GetChannel()
    {
        var connection = GetConnection();
        if (_channel == null || _channel.IsClosed)
        {
            _channel = connection.CreateModel();
        }
        return _channel;
    }

    private void CreateConnection()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            
            _logger.LogInformation(
                "Connected to RabbitMQ at {HostName}:{Port}", 
                _settings.HostName, _settings.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}