using CrudService.Api.Controllers;
using CrudService.Application.UseCases.Waitlist.EnrollInWaitlist;
using CrudService.Application.UseCases.Waitlist.GetWaitlistStatus;
using CrudService.Infrastructure.Sse;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace CrudService.Api.Tests.Waitlist;

public class WaitlistStreamEndpointTests
{
    private readonly IWaitlistSseSubscriber _subscriber = Substitute.For<IWaitlistSseSubscriber>();
    private readonly IEnrollInWaitlistUseCase _enrollUseCase = Substitute.For<IEnrollInWaitlistUseCase>();
    private readonly IGetWaitlistStatusUseCase _statusUseCase = Substitute.For<IGetWaitlistStatusUseCase>();

    private WaitlistController CreateController()
    {
        var controller = new WaitlistController(_enrollUseCase, _statusUseCase, _subscriber);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.HttpContext.RequestAborted = new CancellationTokenSource().Token;
        return controller;
    }

    [Fact]
    public async Task StreamSse_SetsCorrectHeaders_AndCallsRegisterAsync()
    {
        _subscriber.GetConnectionCount("test@example.com").Returns(0);
        _subscriber.RegisterAsync("test@example.com", Arg.Any<HttpResponse>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var client = new SseClient("test@example.com", ci.ArgAt<HttpResponse>(1), ci.ArgAt<CancellationToken>(2));
                return client;
            });

        var controller = CreateController();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        controller.HttpContext.RequestAborted = cts.Token;

        await controller.StreamSse("test@example.com");

        var response = controller.HttpContext.Response;
        Assert.Equal("text/event-stream", response.ContentType);
        Assert.Equal("no-cache", response.Headers.CacheControl.ToString());
        Assert.Equal("keep-alive", response.Headers.Connection.ToString());

        await _subscriber.Received(1).RegisterAsync("test@example.com", Arg.Any<HttpResponse>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@domain.com")]
    public async Task StreamSse_InvalidEmail_Returns400(string email)
    {
        var controller = CreateController();

        var result = await controller.StreamSse(email);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task StreamSse_NullEmail_Returns400()
    {
        var controller = CreateController();

        var result = await controller.StreamSse(null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task StreamSse_ConnectionLimitReached_Returns429()
    {
        _subscriber.GetConnectionCount("busy@test.com").Returns(5);

        var controller = CreateController();

        var result = await controller.StreamSse("busy@test.com");

        Assert.IsType<ObjectResult>(result);
        var objectResult = (ObjectResult)result;
        Assert.Equal(429, objectResult.StatusCode);
    }

    [Fact]
    public async Task StreamSse_EmitsKeepaliveComment_WhileConnectionOpen()
    {
        _subscriber.GetConnectionCount("keep@test.com").Returns(0);
        _subscriber.RegisterAsync("keep@test.com", Arg.Any<HttpResponse>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var client = new SseClient("keep@test.com", ci.ArgAt<HttpResponse>(1), ci.ArgAt<CancellationToken>(2));
                return client;
            });

        var controller = CreateController();
        var memoryStream = new MemoryStream();
        controller.HttpContext.Response.Body = memoryStream;

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        controller.HttpContext.RequestAborted = cts.Token;

        // StreamSse with very short keepalive interval should emit at least one keepalive
        Environment.SetEnvironmentVariable("SSE_KEEPALIVE_INTERVAL_SECONDS", "0.1");
        try
        {
            await controller.StreamSse("keep@test.com");
        }
        finally
        {
            Environment.SetEnvironmentVariable("SSE_KEEPALIVE_INTERVAL_SECONDS", null);
        }

        memoryStream.Position = 0;
        var output = new StreamReader(memoryStream).ReadToEnd();
        Assert.Contains(": keepalive", output);
    }

    [Fact]
    public async Task StreamSse_Reconnection_DoesNotReplayPastEvents()
    {
        _subscriber.GetConnectionCount("recon@test.com").Returns(0);

        var firstClient = new SseClient("recon@test.com",
            new DefaultHttpContext().Response,
            CancellationToken.None);

        var secondClient = new SseClient("recon@test.com",
            new DefaultHttpContext().Response,
            CancellationToken.None);

        var callCount = 0;
        _subscriber.RegisterAsync("recon@test.com", Arg.Any<HttpResponse>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++callCount == 1 ? firstClient : secondClient);

        // First connection
        var controller1 = CreateController();
        using var cts1 = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        controller1.HttpContext.RequestAborted = cts1.Token;
        await controller1.StreamSse("recon@test.com");

        // Second connection (reconnection)
        var controller2 = CreateController();
        var memoryStream = new MemoryStream();
        controller2.HttpContext.Response.Body = memoryStream;
        using var cts2 = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        controller2.HttpContext.RequestAborted = cts2.Token;
        await controller2.StreamSse("recon@test.com");

        // Second connection should have no SSE event data (no replay)
        memoryStream.Position = 0;
        var output = new StreamReader(memoryStream).ReadToEnd();
        Assert.DoesNotContain("event:", output);
    }
}
