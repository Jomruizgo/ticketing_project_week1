using MsPaymentService.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddHostedService<MsPaymentService.Worker.Worker>();

var host = builder.Build();

host.Run();
