using CrudService.Infrastructure.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CrudService.Infrastructure.Tests.Component;

/// <summary>
/// Component tests — testing pyramid: COMPONENT level.
/// Tests the full messaging pipeline: JSON → TicketStatusConsumer.ProcessMessage → TicketStatusHub → ChannelReader.
/// No mocks: real Consumer + real Hub wired together as a single component.
/// </summary>
[Trait("Category", "Component")]
public class TicketStatusPipelineTests
{
    private readonly TicketStatusHub _hub;
    private readonly TicketStatusConsumer _consumer;

    public TicketStatusPipelineTests()
    {
        _hub = new TicketStatusHub();
        var options = Options.Create(new RabbitMQSettings());
        _consumer = new TicketStatusConsumer(_hub, options, NullLogger<TicketStatusConsumer>.Instance);
    }

    [Fact]
    public async Task Pipeline_ReleasedStatus_FlowsFromJsonToChannelReader()
    {
        // Arrange: subscriber waiting for ticket 1
        var reader = _hub.Subscribe(1);

        // Act: simulate a RabbitMQ message with "released" status
        _consumer.ProcessMessage("""
        {
          "ticketId": 1,
          "newStatus": "released",
          "changedAt": "2026-02-24T12:00:00Z"
        }
        """);

        // Assert: the subscriber receives the correct update
        var update = await reader.ReadAsync(CancellationToken.None);
        Assert.Equal(1, update.TicketId);
        Assert.Equal("released", update.NewStatus);
    }

    [Fact]
    public async Task Pipeline_PaidStatus_FlowsFromJsonToChannelReader()
    {
        var reader = _hub.Subscribe(2);

        _consumer.ProcessMessage("""
        {
          "ticketId": 2,
          "newStatus": "paid",
          "changedAt": "2026-02-24T13:00:00Z"
        }
        """);

        var update = await reader.ReadAsync(CancellationToken.None);
        Assert.Equal(2, update.TicketId);
        Assert.Equal("paid", update.NewStatus);
    }

    [Fact]
    public async Task Pipeline_MultipleTickets_EachRoutedToCorrectSubscriber()
    {
        // Arrange: 3 different ticket subscriptions
        var reader10 = _hub.Subscribe(10);
        var reader20 = _hub.Subscribe(20);
        var reader30 = _hub.Subscribe(30);

        // Act: 3 different messages arrive (in any order)
        _consumer.ProcessMessage("""{"ticketId":20,"newStatus":"paid","changedAt":"2026-02-24T12:00:00Z"}""");
        _consumer.ProcessMessage("""{"ticketId":10,"newStatus":"released","changedAt":"2026-02-24T12:01:00Z"}""");
        _consumer.ProcessMessage("""{"ticketId":30,"newStatus":"reserved","changedAt":"2026-02-24T12:02:00Z"}""");

        // Assert: each subscriber received its correct update
        var u10 = await reader10.ReadAsync(CancellationToken.None);
        Assert.Equal(10, u10.TicketId);
        Assert.Equal("released", u10.NewStatus);

        var u20 = await reader20.ReadAsync(CancellationToken.None);
        Assert.Equal(20, u20.TicketId);
        Assert.Equal("paid", u20.NewStatus);

        var u30 = await reader30.ReadAsync(CancellationToken.None);
        Assert.Equal(30, u30.TicketId);
        Assert.Equal("reserved", u30.NewStatus);
    }

    [Fact]
    public async Task Pipeline_InvalidThenValidMessage_OnlyValidDelivered()
    {
        var reader = _hub.Subscribe(40);

        // Act: first an invalid message, then a valid one
        _consumer.ProcessMessage("{ malformed }}}");
        _consumer.ProcessMessage("""
        {
          "ticketId": 40,
          "newStatus": "released",
          "changedAt": "2026-02-24T14:00:00Z"
        }
        """);

        // Assert: subscriber only receives the valid message
        var update = await reader.ReadAsync(CancellationToken.None);
        Assert.Equal(40, update.TicketId);
        Assert.Equal("released", update.NewStatus);
    }

    [Fact]
    public void Pipeline_WithoutSubscriber_CompletesWithoutError()
    {
        // No subscriber registered for ticket 99
        var ex = Record.Exception(() =>
            _consumer.ProcessMessage("""
            {
              "ticketId": 99,
              "newStatus": "released",
              "changedAt": "2026-02-24T15:00:00Z"
            }
            """)
        );

        Assert.Null(ex);
    }

    [Fact]
    public async Task Pipeline_MultipleSubscribersSameTicket_AllReceive()
    {
        // Arrange: 3 subscribers for the same ticket
        var reader1 = _hub.Subscribe(50);
        var reader2 = _hub.Subscribe(50);
        var reader3 = _hub.Subscribe(50);

        // Act
        _consumer.ProcessMessage("""
        {
          "ticketId": 50,
          "newStatus": "released",
          "changedAt": "2026-02-24T16:00:00Z"
        }
        """);

        // Assert: each subscriber received the same update
        var u1 = await reader1.ReadAsync(CancellationToken.None);
        var u2 = await reader2.ReadAsync(CancellationToken.None);
        var u3 = await reader3.ReadAsync(CancellationToken.None);

        Assert.All(new[] { u1, u2, u3 }, u =>
        {
            Assert.Equal(50, u.TicketId);
            Assert.Equal("released", u.NewStatus);
        });
    }

    [Fact]
    public async Task Pipeline_SequentialMessages_SubscriptionCleanedUpBetween()
    {
        // First subscription + delivery
        var reader1 = _hub.Subscribe(60);
        _consumer.ProcessMessage("""{"ticketId":60,"newStatus":"reserved","changedAt":"2026-02-24T17:00:00Z"}""");
        var u1 = await reader1.ReadAsync(CancellationToken.None);
        Assert.Equal("reserved", u1.NewStatus);

        // After delivery, subscription is removed. New subscription for same ticket.
        var reader2 = _hub.Subscribe(60);
        _consumer.ProcessMessage("""{"ticketId":60,"newStatus":"released","changedAt":"2026-02-24T17:01:00Z"}""");
        var u2 = await reader2.ReadAsync(CancellationToken.None);
        Assert.Equal("released", u2.NewStatus);
    }

    [Fact]
    public async Task Pipeline_ConcurrentMessages_AllDeliveredCorrectly()
    {
        // Arrange: 20 tickets with subscriptions
        const int count = 20;
        var readers = Enumerable.Range(1, count)
            .Select(i => (Id: (long)(1000 + i), Reader: _hub.Subscribe(1000 + i)))
            .ToList();

        // Act: process all messages (synchronous but testing throughput)
        foreach (var (id, _) in readers)
        {
            _consumer.ProcessMessage(
                $$"""{"ticketId":{{id}},"newStatus":"released","changedAt":"2026-02-24T18:00:00Z"}""");
        }

        // Assert: all readers received their updates
        foreach (var (id, reader) in readers)
        {
            var update = await reader.ReadAsync(CancellationToken.None);
            Assert.Equal(id, update.TicketId);
            Assert.Equal("released", update.NewStatus);
        }
    }
}
