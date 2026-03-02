using System.Text.Json;
using CrudService.Infrastructure.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CrudService.Infrastructure.Tests.Integration;

/// <summary>
/// Integration tests — testing pyramid: INTEGRATION level.
/// Verifies the end-to-end data transformation from RabbitMQ message payload
/// through the messaging pipeline to the SSE output format consumed by the frontend.
/// Tests the contract between services and the SSE data format.
/// </summary>
[Trait("Category", "Integration")]
public class SseContractIntegrationTests
{
    private readonly TicketStatusHub _hub;
    private readonly TicketStatusConsumer _consumer;

    public SseContractIntegrationTests()
    {
        _hub = new TicketStatusHub();
        var options = Options.Create(new RabbitMQSettings());
        _consumer = new TicketStatusConsumer(_hub, options, NullLogger<TicketStatusConsumer>.Instance);
    }

    /// <summary>
    /// Verifies the SSE JSON contract matches what the frontend expects.
    /// Controller serializes: JsonSerializer.Serialize(new { ticketId, status })
    /// Frontend expects: { "ticketId": N, "status": "released" }
    /// </summary>
    [Theory]
    [InlineData("released")]
    [InlineData("paid")]
    [InlineData("reserved")]
    public async Task SseOutput_StatusValues_MatchExpectedJsonContract(string status)
    {
        // Arrange
        var reader = _hub.Subscribe(1);

        // Act: simulate RabbitMQ message arrival
        _consumer.ProcessMessage(
            $$"""{"ticketId":1,"newStatus":"{{status}}","changedAt":"2026-02-24T12:00:00Z"}""");

        // Get the update from the hub (what the controller would receive)
        var update = await reader.ReadAsync(CancellationToken.None);

        // Serialize using the extracted SseMessageFormatter (same as controller)
        var sseJson = SseMessageFormatter.ToSseJson(update);
        var parsed = JsonDocument.Parse(sseJson);
        var root = parsed.RootElement;

        // Assert: SSE JSON contract
        Assert.Equal(1, root.GetProperty("ticketId").GetInt64());
        Assert.Equal(status, root.GetProperty("status").GetString());
        Assert.Equal(2, root.EnumerateObject().Count()); // Only ticketId + status
    }

    /// <summary>
    /// Verifies the SSE output uses the exact field names the frontend expects.
    /// Contract: "ticketId" (camelCase) and "status" (not "newStatus").
    /// </summary>
    [Fact]
    public async Task SseOutput_FieldNames_AreCamelCasePerContract()
    {
        var reader = _hub.Subscribe(100);

        _consumer.ProcessMessage("""
        {"ticketId":100,"newStatus":"released","changedAt":"2026-02-24T12:00:00Z"}
        """);

        var update = await reader.ReadAsync(CancellationToken.None);
        var sseJson = SseMessageFormatter.ToSseJson(update);

        // Field names must be lowercase camelCase
        Assert.Contains("\"ticketId\"", sseJson);
        Assert.Contains("\"status\"", sseJson);
        // Must NOT expose the internal field name "newStatus"
        Assert.DoesNotContain("\"newStatus\"", sseJson);
        Assert.DoesNotContain("\"NewStatus\"", sseJson);
    }

    /// <summary>
    /// Verifies the incoming RabbitMQ payload contract fields are correctly mapped.
    /// The consumer expects: ticketId (long), newStatus (string), changedAt (DateTime).
    /// The SSE output transforms: newStatus → status.
    /// </summary>
    [Fact]
    public async Task PayloadContract_IncomingFieldsMapToSseOutput()
    {
        var reader = _hub.Subscribe(200);

        // Incoming payload from ReservationService/PaymentService
        var incomingPayload = new
        {
            ticketId = 200L,
            newStatus = "released",
            changedAt = "2026-02-24T12:00:00Z"
        };

        _consumer.ProcessMessage(JsonSerializer.Serialize(incomingPayload));

        var update = await reader.ReadAsync(CancellationToken.None);

        // Outgoing SSE (as controller formats it via SseMessageFormatter)
        var sseJson = SseMessageFormatter.ToSseJson(update);

        // Assert transformation: newStatus in → status out
        Assert.Equal(200, update.TicketId);
        Assert.Equal("released", update.NewStatus);
        Assert.Contains("\"status\":\"released\"", sseJson);
    }

    /// <summary>
    /// Verifies SSE event format follows Server-Sent Events spec.
    /// Format: "data: {json}\n\n"
    /// </summary>
    [Fact]
    public async Task SseFormat_FollowsServerSentEventsSpec()
    {
        var reader = _hub.Subscribe(300);

        _consumer.ProcessMessage("""
        {"ticketId":300,"newStatus":"released","changedAt":"2026-02-24T12:00:00Z"}
        """);

        var update = await reader.ReadAsync(CancellationToken.None);
        var sseMessage = SseMessageFormatter.FormatSseLine(update);

        // SSE spec compliance
        Assert.StartsWith("data: ", sseMessage);
        Assert.EndsWith("\n\n", sseMessage);
        Assert.Single(sseMessage.Split("data: ", StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Verifies full data integrity: the ticketId and status values
    /// are preserved end-to-end from RabbitMQ to SSE without mutation.
    /// </summary>
    [Theory]
    [InlineData(1, "released")]
    [InlineData(999999, "paid")]
    [InlineData(0, "reserved")]
    public async Task FullFlow_DataIntegrity_PreservedEndToEnd(long ticketId, string status)
    {
        var reader = _hub.Subscribe(ticketId);

        var rabbitPayload = JsonSerializer.Serialize(new
        {
            ticketId,
            newStatus = status,
            changedAt = DateTime.UtcNow
        });

        _consumer.ProcessMessage(rabbitPayload);

        var update = await reader.ReadAsync(CancellationToken.None);

        // Data integrity: values unchanged from input to output
        Assert.Equal(ticketId, update.TicketId);
        Assert.Equal(status, update.NewStatus);
    }

    /// <summary>
    /// Verifies the pipeline is resilient: a sequence of mixed valid/invalid
    /// messages doesn't corrupt state for subsequent valid messages.
    /// </summary>
    [Fact]
    public async Task Pipeline_Resilience_InvalidMessagesDoNotCorruptState()
    {
        // Send several invalid messages interspersed with valid ones
        _consumer.ProcessMessage("");
        _consumer.ProcessMessage("null");
        _consumer.ProcessMessage("{invalid}");
        _consumer.ProcessMessage("""{"ticketId":400,"newStatus":"","changedAt":"2026-02-24T12:00:00Z"}""");

        // Now subscribe and send a valid message
        var reader = _hub.Subscribe(500);
        _consumer.ProcessMessage("""{"ticketId":500,"newStatus":"released","changedAt":"2026-02-24T13:00:00Z"}""");

        var update = await reader.ReadAsync(CancellationToken.None);
        Assert.Equal(500, update.TicketId);
        Assert.Equal("released", update.NewStatus);
    }
}
