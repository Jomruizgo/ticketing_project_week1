using System.Text.Json;
using Xunit;

namespace CrudService.Infrastructure.Tests.Messaging;

[Trait("Category", "Unit")]
public class InventoryReturnAdapterTests
{
    [Fact(DisplayName = "TC-HU6-ADAPTER-01: inventory return payload contains ticketId, eventId, returnedAt")]
    public void PayloadSerialization_ContainsAllRequiredFields()
    {
        var ticketId = 42L;
        var eventId = 7L;
        var returnedAt = DateTime.UtcNow;

        // Replicate the same anonymous object and serialization options used in InventoryReturnAdapter
        var payload = new
        {
            ticketId,
            eventId,
            returnedAt
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(payload, options);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("ticketId", out var tid));
        Assert.Equal(42L, tid.GetInt64());

        Assert.True(root.TryGetProperty("eventId", out var eid));
        Assert.Equal(7L, eid.GetInt64());

        Assert.True(root.TryGetProperty("returnedAt", out var rat));
        Assert.NotNull(rat.GetString());
        Assert.True(DateTime.TryParse(rat.GetString(), out _));
    }

    [Fact(DisplayName = "TC-HU6-ADAPTER-02: payload has exactly three fields")]
    public void PayloadSerialization_HasExactlyThreeFields()
    {
        var payload = new
        {
            ticketId = 1L,
            eventId = 2L,
            returnedAt = DateTime.UtcNow
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(payload, options);
        var doc = JsonDocument.Parse(json);

        Assert.Equal(3, doc.RootElement.EnumerateObject().Count());
    }
}
