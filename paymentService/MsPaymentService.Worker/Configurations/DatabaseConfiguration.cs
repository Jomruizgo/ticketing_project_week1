namespace MsPaymentService.Worker.Configurations;

public class DatabaseConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public bool EnableDetailedErrors { get; set; } = false;
    public int CommandTimeout { get; set; } = 30;
}