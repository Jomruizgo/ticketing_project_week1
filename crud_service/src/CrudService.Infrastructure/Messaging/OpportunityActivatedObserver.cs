namespace CrudService.Infrastructure.Messaging;

using System.Text;
using System.Text.Json;
using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

public class OpportunityActivatedObserver : IOpportunityObserver
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<OpportunityActivatedObserver> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OpportunityActivatedObserver(
        IOptions<RabbitMQSettings> settings,
        ILogger<OpportunityActivatedObserver> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task OnOpportunityActivatedAsync(WaitlistOpportunity opportunity)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.Username,
            Password = _settings.Password
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        var activatedEvent = new OpportunityActivatedEvent
        {
            OpportunityId = opportunity.Id,
            WaitlistEntryId = opportunity.WaitlistEntryId,
            TicketId = opportunity.TicketId,
            EventId = opportunity.WaitlistEntry.EventId,
            BuyerEmail = opportunity.WaitlistEntry.BuyerEmail,
            ActivatedAt = opportunity.ActivatedAt!.Value,
            ExpiresAt = opportunity.ExpiresAt!.Value
        };

        var activatedBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(activatedEvent, JsonOptions));
        channel.BasicPublish(
            exchange: "tickets",
            routingKey: "waitlist.opportunity.activated",
            basicProperties: null,
            body: activatedBody);

        _logger.LogInformation(
            "Published waitlist.opportunity.activated for OpportunityId={OpportunityId}, TicketId={TicketId}",
            opportunity.Id, opportunity.TicketId);

        var delayPayload = new { opportunityId = opportunity.Id };
        var delayBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(delayPayload, JsonOptions));
        channel.BasicPublish(
            exchange: "",
            routingKey: "q.waitlist.opportunity.delay",
            basicProperties: null,
            body: delayBody);

        _logger.LogInformation(
            "Published delay message for OpportunityId={OpportunityId} to q.waitlist.opportunity.delay",
            opportunity.Id);

        await Task.CompletedTask;
    }
}
