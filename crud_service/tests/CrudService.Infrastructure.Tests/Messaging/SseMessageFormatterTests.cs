using System.Text.Json;
using CrudService.Application.Interfaces;
using CrudService.Infrastructure.Messaging;

namespace CrudService.Infrastructure.Tests.Messaging;

/// <summary>
/// Unit tests for SseMessageFormatter — testing pyramid: UNIT level.
/// Validates the SSE serialization contract after REFACTOR extraction.
/// </summary>
[Trait("Category", "Unit")]
public class SseMessageFormatterTests
{
    [Fact]
    public void ToSseJson_ReleasedStatus_ReturnsCorrectJson()
    {
        var update = new TicketStatusUpdate(42, "released");

        var json = SseMessageFormatter.ToSseJson(update);

        var doc = JsonDocument.Parse(json);
        Assert.Equal(42, doc.RootElement.GetProperty("ticketId").GetInt64());
        Assert.Equal("released", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public void ToSseJson_PaidStatus_ReturnsCorrectJson()
    {
        var update = new TicketStatusUpdate(99, "paid");

        var json = SseMessageFormatter.ToSseJson(update);

        var doc = JsonDocument.Parse(json);
        Assert.Equal(99, doc.RootElement.GetProperty("ticketId").GetInt64());
        Assert.Equal("paid", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public void ToSseJson_OnlyContainsContractFields()
    {
        var update = new TicketStatusUpdate(1, "released");

        var json = SseMessageFormatter.ToSseJson(update);
        var doc = JsonDocument.Parse(json);

        // Contract: only "ticketId" and "status"
        var properties = doc.RootElement.EnumerateObject().Select(p => p.Name).ToList();
        Assert.Equal(2, properties.Count);
        Assert.Contains("ticketId", properties);
        Assert.Contains("status", properties);
    }

    [Fact]
    public void ToSseJson_DoesNotExposeInternalFieldName()
    {
        var update = new TicketStatusUpdate(1, "released");

        var json = SseMessageFormatter.ToSseJson(update);

        // "newStatus" is internal, SSE uses "status"
        Assert.DoesNotContain("newStatus", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormatSseLine_FollowsSseSpec()
    {
        var update = new TicketStatusUpdate(50, "released");

        var line = SseMessageFormatter.FormatSseLine(update);

        Assert.StartsWith("data: ", line);
        Assert.EndsWith("\n\n", line);
    }

    [Fact]
    public void FormatSseLine_ContainsValidJson()
    {
        var update = new TicketStatusUpdate(50, "released");

        var line = SseMessageFormatter.FormatSseLine(update);

        // Extract JSON from "data: {json}\n\n"
        var json = line["data: ".Length..^2];
        var doc = JsonDocument.Parse(json);
        Assert.Equal(50, doc.RootElement.GetProperty("ticketId").GetInt64());
        Assert.Equal("released", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public void ToSseJson_NullUpdate_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SseMessageFormatter.ToSseJson(null!));
    }
}
