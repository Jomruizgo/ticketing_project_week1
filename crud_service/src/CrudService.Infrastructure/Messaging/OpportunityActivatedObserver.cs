namespace CrudService.Infrastructure.Messaging;

using System.Text;
using System.Text.Json;
using CrudService.Domain.Entities;
using CrudService.Domain.Events;
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

    public async Task OnOpportunityActivatedAsync(OpportunityActivatedEvent activatedEvent)
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

        var rabbitPayload = new
        {
            opportunityId = activatedEvent.OpportunityId,
            waitlistEntryId = 0L,
            ticketId = 0L,
            eventId = activatedEvent.EventId,
            buyerEmail = activatedEvent.BuyerEmail,
            activatedAt = activatedEvent.ActivatedAt,
            expiresAt = activatedEvent.ExpiresAt
        };

        var activatedBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(rabbitPayload, JsonOptions));
        channel.BasicPublish(
            exchange: "tickets",
            routingKey: "waitlist.opportunity.activated",
            basicProperties: null,
            body: activatedBody);

        _logger.LogInformation(
            "Published waitlist.opportunity.activated for OpportunityId={OpportunityId}",
            activatedEvent.OpportunityId);

        var delayPayload = new { opportunityId = activatedEvent.OpportunityId };
        var delayBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(delayPayload, JsonOptions));
        channel.BasicPublish(
            exchange: "",
            routingKey: "q.waitlist.opportunity.delay",
            basicProperties: null,
            body: delayBody);

        _logger.LogInformation(
            "Published delay message for OpportunityId={OpportunityId} to q.waitlist.opportunity.delay",
            activatedEvent.OpportunityId);

        await Task.CompletedTask;
    }

    public async Task OnOpportunityExpiredAsync(WaitlistOpportunity opportunity)
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

        var payload = new
        {
            opportunityId = opportunity.Id,
            waitlistEntryId = opportunity.WaitlistEntryId,
            ticketId = opportunity.TicketId,
            buyerEmail = opportunity.WaitlistEntry?.BuyerEmail,
            expiredAt = opportunity.ExpiredAt,
            expirationReason = opportunity.ExpirationReason
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOptions));
        channel.BasicPublish(
            exchange: "tickets",
            routingKey: "waitlist.opportunity.expired",
            basicProperties: null,
            body: body);

        _logger.LogInformation(
            "Published waitlist.opportunity.expired for OpportunityId={OpportunityId}",
            opportunity.Id);

        await Task.CompletedTask;
    }
}
