using CrudService.Infrastructure.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace CrudService.Infrastructure.Tests.Messaging;

/// <summary>
/// Unit tests for TicketStatusConsumer.ProcessMessage — testing pyramid: UNIT level.
/// DIP: tests use both real Hub and mocked ITicketStatusNotifier to prove
/// the consumer depends on the abstraction, not the concrete implementation.
/// </summary>
[Trait("Category", "Unit")]
public class TicketStatusConsumerTests
{
    private readonly TicketStatusHub _hub = new();
    private readonly TicketStatusConsumer _consumer;

    public TicketStatusConsumerTests()
    {
        var options = Options.Create(new RabbitMQSettings());
        // DIP: TicketStatusHub implements ITicketStatusNotifier — consumer accepts the interface.
        _consumer = new TicketStatusConsumer(_hub, options, NullLogger<TicketStatusConsumer>.Instance);
    }

    // ── Existing unit tests (already GREEN) ───────────────────────────

    [Fact]
    public async Task ProcessMessage_WhenNewStatusReleased_NotifiesHubSubscribers()
    {
        var reader = _hub.Subscribe(42);

        _consumer.ProcessMessage("""
        {
          "ticketId": 42,
          "newStatus": "released",
          "changedAt": "2026-02-24T12:00:00Z"
        }
        """);

        var update = await reader.ReadAsync(CancellationToken.None);

        Assert.Equal(42, update.TicketId);
        Assert.Equal("released", update.NewStatus);
    }

    [Fact]
    public void ProcessMessage_WhenStatusIsEmpty_DoesNotNotify()
    {
        var reader = _hub.Subscribe(42);

        _consumer.ProcessMessage("""
        {
          "ticketId": 42,
          "newStatus": "   ",
          "changedAt": "2026-02-24T12:00:00Z"
        }
        """);

        Assert.False(reader.TryRead(out _));
    }

    // ── RED: New unit tests — edge cases & additional statuses ─────────

    [Fact]
    public async Task ProcessMessage_WhenNewStatusPaid_NotifiesHub()
    {
        var reader = _hub.Subscribe(10);

        _consumer.ProcessMessage("""
        {
          "ticketId": 10,
          "newStatus": "paid",
          "changedAt": "2026-02-24T13:00:00Z"
        }
        """);

        var update = await reader.ReadAsync(CancellationToken.None);
        Assert.Equal(10, update.TicketId);
        Assert.Equal("paid", update.NewStatus);
    }

    [Fact]
    public async Task ProcessMessage_WhenNewStatusReserved_NotifiesHub()
    {
        var reader = _hub.Subscribe(20);

        _consumer.ProcessMessage("""
        {
          "ticketId": 20,
          "newStatus": "reserved",
          "changedAt": "2026-02-24T14:00:00Z"
        }
        """);

        var update = await reader.ReadAsync(CancellationToken.None);
        Assert.Equal(20, update.TicketId);
        Assert.Equal("reserved", update.NewStatus);
    }

    [Fact]
    public void ProcessMessage_WhenInvalidJson_DoesNotThrow()
    {
        // Defensive: malformed JSON must not crash the consumer
        var ex = Record.Exception(() => _consumer.ProcessMessage("not-valid-json{{{"));
        Assert.Null(ex);
    }

    [Fact]
    public void ProcessMessage_WhenEmptyString_DoesNotThrow()
    {
        // Defensive: empty payload must not crash the consumer
        var ex = Record.Exception(() => _consumer.ProcessMessage(""));
        Assert.Null(ex);
    }

    [Fact]
    public void ProcessMessage_WhenNullJsonLiteral_DoesNotThrow()
    {
        // Defensive: JSON "null" deserializes to null object
        var ex = Record.Exception(() => _consumer.ProcessMessage("null"));
        Assert.Null(ex);
    }

    [Fact]
    public void ProcessMessage_WhenNullString_DoesNotThrow()
    {
        // Defensive: null reference must not crash the consumer
        var ex = Record.Exception(() => _consumer.ProcessMessage(null!));
        Assert.Null(ex);
    }

    [Fact]
    public void ProcessMessage_WhenMissingNewStatus_DoesNotNotify()
    {
        // JSON with ticketId but no newStatus → should be treated as empty status
        var reader = _hub.Subscribe(50);

        _consumer.ProcessMessage("""
        {
          "ticketId": 50,
          "changedAt": "2026-02-24T15:00:00Z"
        }
        """);

        Assert.False(reader.TryRead(out _));
    }

    [Fact]
    public async Task ProcessMessage_CaseInsensitiveDeserialization_Works()
    {
        // Contract: property names should be case-insensitive
        var reader = _hub.Subscribe(77);

        _consumer.ProcessMessage("""
        {
          "TicketId": 77,
          "NewStatus": "released",
          "ChangedAt": "2026-02-24T16:00:00Z"
        }
        """);

        var update = await reader.ReadAsync(CancellationToken.None);
        Assert.Equal(77, update.TicketId);
        Assert.Equal("released", update.NewStatus);
    }

    // ── DIP: Tests with mocked ITicketStatusNotifier ──────────────────

    [Fact]
    public void ProcessMessage_WithMockedNotifier_CallsNotifyWithCorrectArgs()
    {
        // DIP proof: consumer works with ANY ITicketStatusNotifier, not just TicketStatusHub
        var mockNotifier = Substitute.For<ITicketStatusNotifier>();
        var options = Options.Create(new RabbitMQSettings());
        var consumer = new TicketStatusConsumer(mockNotifier, options, NullLogger<TicketStatusConsumer>.Instance);

        consumer.ProcessMessage("""
        {"ticketId":42,"newStatus":"released","changedAt":"2026-02-24T12:00:00Z"}
        """);

        mockNotifier.Received(1).Notify(42, "released");
    }

    [Fact]
    public void ProcessMessage_WithMockedNotifier_EmptyStatus_DoesNotCallNotify()
    {
        var mockNotifier = Substitute.For<ITicketStatusNotifier>();
        var options = Options.Create(new RabbitMQSettings());
        var consumer = new TicketStatusConsumer(mockNotifier, options, NullLogger<TicketStatusConsumer>.Instance);

        consumer.ProcessMessage("""
        {"ticketId":42,"newStatus":"","changedAt":"2026-02-24T12:00:00Z"}
        """);

        mockNotifier.DidNotReceive().Notify(Arg.Any<long>(), Arg.Any<string>());
    }

    [Fact]
    public void ProcessMessage_WithMockedNotifier_InvalidJson_DoesNotCallNotify()
    {
        var mockNotifier = Substitute.For<ITicketStatusNotifier>();
        var options = Options.Create(new RabbitMQSettings());
        var consumer = new TicketStatusConsumer(mockNotifier, options, NullLogger<TicketStatusConsumer>.Instance);

        consumer.ProcessMessage("broken{json");

        mockNotifier.DidNotReceive().Notify(Arg.Any<long>(), Arg.Any<string>());
    }
}
