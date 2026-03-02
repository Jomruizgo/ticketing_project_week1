using CrudService.Infrastructure.Messaging;

namespace CrudService.Infrastructure.Tests.Messaging;

/// <summary>
/// Unit tests for StatusPayloadParser — testing pyramid: UNIT level.
/// Validates unified payload deserialization after REFACTOR extraction.
/// </summary>
[Trait("Category", "Unit")]
public class StatusPayloadParserTests
{
    [Fact]
    public void TryParse_ValidJson_ReturnsPayload()
    {
        var result = StatusPayloadParser.TryParse("""
        {"ticketId":42,"newStatus":"released","changedAt":"2026-02-24T12:00:00Z"}
        """);

        Assert.NotNull(result);
        Assert.Equal(42, result.TicketId);
        Assert.Equal("released", result.NewStatus);
    }

    [Fact]
    public void TryParse_CaseInsensitive_ReturnsPayload()
    {
        var result = StatusPayloadParser.TryParse("""
        {"TicketId":10,"NewStatus":"paid","ChangedAt":"2026-02-24T12:00:00Z"}
        """);

        Assert.NotNull(result);
        Assert.Equal(10, result.TicketId);
        Assert.Equal("paid", result.NewStatus);
    }

    [Fact]
    public void TryParse_NullInput_ReturnsNull()
    {
        Assert.Null(StatusPayloadParser.TryParse(null));
    }

    [Fact]
    public void TryParse_EmptyString_ReturnsNull()
    {
        Assert.Null(StatusPayloadParser.TryParse(""));
    }

    [Fact]
    public void TryParse_WhitespaceOnly_ReturnsNull()
    {
        Assert.Null(StatusPayloadParser.TryParse("   "));
    }

    [Fact]
    public void TryParse_InvalidJson_ReturnsNull()
    {
        Assert.Null(StatusPayloadParser.TryParse("not-json{{{"));
    }

    [Fact]
    public void TryParse_JsonNull_ReturnsNull()
    {
        Assert.Null(StatusPayloadParser.TryParse("null"));
    }

    [Fact]
    public void TryParse_MissingNewStatus_ReturnsPayloadWithDefaultStatus()
    {
        var result = StatusPayloadParser.TryParse("""
        {"ticketId":50,"changedAt":"2026-02-24T12:00:00Z"}
        """);

        Assert.NotNull(result);
        Assert.Equal(50, result.TicketId);
        Assert.Null(result.NewStatus);
    }

    [Theory]
    [InlineData("released")]
    [InlineData("paid")]
    [InlineData("reserved")]
    public void TryParse_AllValidStatuses_ParseCorrectly(string status)
    {
        var result = StatusPayloadParser.TryParse(
            $$"""{"ticketId":1,"newStatus":"{{status}}","changedAt":"2026-02-24T12:00:00Z"}""");

        Assert.NotNull(result);
        Assert.Equal(status, result.NewStatus);
    }
}
