using MsPaymentService.Worker.Handlers;
using MsPaymentService.Worker.Models.DTOs;
using NSubstitute;
using Xunit;

namespace MsPaymentService.Worker.Tests;

public class PaymentEventDispatcherImplTests
{
    [Fact]
    public async Task DispatchAsync_MatchingHandler_DelegatesToHandler()
    {
        var handler = Substitute.For<IPaymentEventHandler>();
        handler.QueueName.Returns("ticket.payments.approved");
        handler.HandleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValidationResult.Success());

        var sut = new PaymentEventDispatcherImpl(new[] { handler });

        var result = await sut.DispatchAsync("ticket.payments.approved", "{}", CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result!.IsSuccess);
        await handler.Received(1).HandleAsync("{}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_NoMatchingHandler_ReturnsNull()
    {
        var handler = Substitute.For<IPaymentEventHandler>();
        handler.QueueName.Returns("ticket.payments.approved");

        var sut = new PaymentEventDispatcherImpl(new[] { handler });

        var result = await sut.DispatchAsync("unknown.queue", "{}", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task DispatchAsync_MultipleHandlers_DispatchesToCorrectOne()
    {
        var approvedHandler = Substitute.For<IPaymentEventHandler>();
        approvedHandler.QueueName.Returns("ticket.payments.approved");
        approvedHandler.HandleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValidationResult.Success());

        var rejectedHandler = Substitute.For<IPaymentEventHandler>();
        rejectedHandler.QueueName.Returns("ticket.payments.rejected");
        rejectedHandler.HandleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValidationResult.Failure("rejected"));

        var sut = new PaymentEventDispatcherImpl(new[] { approvedHandler, rejectedHandler });

        var result = await sut.DispatchAsync("ticket.payments.rejected", "{}", CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.IsSuccess);
        await approvedHandler.DidNotReceive().HandleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await rejectedHandler.Received(1).HandleAsync("{}", Arg.Any<CancellationToken>());
    }
}
