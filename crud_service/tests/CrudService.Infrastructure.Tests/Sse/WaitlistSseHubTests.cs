using CrudService.Infrastructure.Sse;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace CrudService.Infrastructure.Tests.Sse;

public class WaitlistSseHubTests
{
    private readonly WaitlistSseHub _hub = new();

    private static (HttpResponse response, CancellationToken token) CreateFakeContext()
    {
        var response = Substitute.For<HttpResponse>();
        var cts = new CancellationTokenSource();
        return (response, cts.Token);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsClientWithCorrectEmailAndUniqueId()
    {
        var (response, token) = CreateFakeContext();

        var client = await _hub.RegisterAsync("buyer@test.com", response, token);

        Assert.NotNull(client);
        Assert.Equal("buyer@test.com", client.Email);
        Assert.False(string.IsNullOrEmpty(client.Id));
    }

    [Fact]
    public async Task RegisterAsync_TwoClients_HaveDifferentIds()
    {
        var (r1, t1) = CreateFakeContext();
        var (r2, t2) = CreateFakeContext();

        var c1 = await _hub.RegisterAsync("buyer@test.com", r1, t1);
        var c2 = await _hub.RegisterAsync("buyer@test.com", r2, t2);

        Assert.NotEqual(c1.Id, c2.Id);
    }

    [Fact]
    public async Task UnregisterAsync_RemovesClient_GetConnectionCountReturnsZero()
    {
        var (response, token) = CreateFakeContext();
        var client = await _hub.RegisterAsync("buyer@test.com", response, token);

        await _hub.UnregisterAsync(client);

        Assert.Equal(0, _hub.GetConnectionCount("buyer@test.com"));
    }

    [Fact]
    public async Task SendEventAsync_WritesToMatchingEmailClient()
    {
        var (response, token) = CreateFakeContext();
        var client = await _hub.RegisterAsync("buyer@test.com", response, token);

        await _hub.SendEventAsync("buyer@test.com", "opportunity_activated", "{\"test\":1}");

        var canRead = client.EventChannel.Reader.TryRead(out var evt);
        Assert.True(canRead);
        Assert.Equal("opportunity_activated", evt!.EventType);
        Assert.Equal("{\"test\":1}", evt.Data);
    }

    [Fact]
    public async Task SendEventAsync_NonMatchingEmail_DoesNotWriteToUnrelatedClients()
    {
        var (response, token) = CreateFakeContext();
        var client = await _hub.RegisterAsync("other@test.com", response, token);

        await _hub.SendEventAsync("buyer@test.com", "opportunity_activated", "{\"test\":1}");

        var canRead = client.EventChannel.Reader.TryRead(out _);
        Assert.False(canRead);
    }

    [Fact]
    public async Task GetConnectionCount_ReturnsCorrectCountForMultipleClientsOnSameEmail()
    {
        var (r1, t1) = CreateFakeContext();
        var (r2, t2) = CreateFakeContext();
        var (r3, t3) = CreateFakeContext();

        await _hub.RegisterAsync("buyer@test.com", r1, t1);
        await _hub.RegisterAsync("buyer@test.com", r2, t2);
        await _hub.RegisterAsync("other@test.com", r3, t3);

        Assert.Equal(2, _hub.GetConnectionCount("buyer@test.com"));
        Assert.Equal(1, _hub.GetConnectionCount("other@test.com"));
        Assert.Equal(0, _hub.GetConnectionCount("nobody@test.com"));
    }
}
