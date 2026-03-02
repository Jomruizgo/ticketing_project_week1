using CrudService.Infrastructure.Messaging;

namespace CrudService.Infrastructure.Tests.Messaging;

/// <summary>
/// Unit tests for TicketStatusHub — testing pyramid: UNIT level.
/// Tests subscribe/notify contract, concurrency, isolation, and guards.
/// </summary>
[Trait("Category", "Unit")]
public class TicketStatusHubTests
{
    // ── Existing unit tests (already GREEN) ───────────────────────────

    [Fact]
    public async Task Notify_WithActiveSubscriber_PublishesReleasedUpdate()
    {
        var hub = new TicketStatusHub();
        var reader = hub.Subscribe(101);

        hub.Notify(101, "released");

        var update = await reader.ReadAsync(CancellationToken.None);

        Assert.Equal(101, update.TicketId);
        Assert.Equal("released", update.NewStatus);
    }

    [Fact]
    public void Notify_WithoutSubscribers_IsSafeNoOp()
    {
        var hub = new TicketStatusHub();

        var ex = Record.Exception(() => hub.Notify(999, "released"));

        Assert.Null(ex);
    }

    // ── RED: New unit tests — edge cases & concurrency ────────────────

    [Fact]
    public async Task Subscribe_MultipleSubscribers_AllReceiveUpdate()
    {
        var hub = new TicketStatusHub();
        var reader1 = hub.Subscribe(200);
        var reader2 = hub.Subscribe(200);

        hub.Notify(200, "released");

        var update1 = await reader1.ReadAsync(CancellationToken.None);
        var update2 = await reader2.ReadAsync(CancellationToken.None);

        Assert.Equal("released", update1.NewStatus);
        Assert.Equal("released", update2.NewStatus);
    }

    [Fact]
    public void Notify_RemovesSubscriptionAfterDelivery()
    {
        var hub = new TicketStatusHub();
        var reader = hub.Subscribe(300);

        hub.Notify(300, "released");

        // Second notify should be no-op (subscription removed)
        var ex = Record.Exception(() => hub.Notify(300, "paid"));
        Assert.Null(ex);

        // Reader should only have the first update
        Assert.True(reader.TryRead(out var update));
        Assert.Equal("released", update.NewStatus);
        Assert.False(reader.TryRead(out _));
    }

    [Fact]
    public void Notify_WithNullStatus_IsNoOp()
    {
        var hub = new TicketStatusHub();
        var reader = hub.Subscribe(400);

        hub.Notify(400, null!);

        Assert.False(reader.TryRead(out _));
    }

    [Fact]
    public async Task Subscribe_DifferentTickets_AreIsolated()
    {
        var hub = new TicketStatusHub();
        var readerA = hub.Subscribe(500);
        var readerB = hub.Subscribe(600);

        hub.Notify(500, "released");

        var updateA = await readerA.ReadAsync(CancellationToken.None);
        Assert.Equal(500, updateA.TicketId);
        Assert.Equal("released", updateA.NewStatus);

        // Ticket B subscriber should NOT receive ticket A notification
        Assert.False(readerB.TryRead(out _));
    }

    [Fact]
    public async Task Notify_ConcurrentCalls_AllSubscribersReceive()
    {
        var hub = new TicketStatusHub();
        const int subscriberCount = 50;

        var readers = Enumerable.Range(0, subscriberCount)
            .Select(i => hub.Subscribe(700 + i))
            .ToList();

        // Notify all tickets concurrently
        var tasks = Enumerable.Range(0, subscriberCount)
            .Select(i => Task.Run(() => hub.Notify(700 + i, "released")))
            .ToArray();

        await Task.WhenAll(tasks);

        foreach (var reader in readers)
        {
            Assert.True(reader.TryRead(out var update));
            Assert.Equal("released", update.NewStatus);
        }
    }

    [Fact]
    public void Subscribe_ReturnsNonNullChannelReader()
    {
        var hub = new TicketStatusHub();
        var reader = hub.Subscribe(800);

        Assert.NotNull(reader);
    }

    [Fact]
    public void Notify_PaidStatus_DeliversCorrectly()
    {
        var hub = new TicketStatusHub();
        var reader = hub.Subscribe(900);

        hub.Notify(900, "paid");

        Assert.True(reader.TryRead(out var update));
        Assert.Equal("paid", update.NewStatus);
    }
}
