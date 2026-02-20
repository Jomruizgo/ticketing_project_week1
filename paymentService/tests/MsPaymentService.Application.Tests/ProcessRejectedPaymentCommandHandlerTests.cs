using Microsoft.Extensions.Logging;
using MsPaymentService.Application.Interfaces;
using MsPaymentService.Application.UseCases.ProcessRejectedPayment;
using MsPaymentService.Domain.Entities;
using MsPaymentService.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace MsPaymentService.Application.Tests;

public class ProcessRejectedPaymentCommandHandlerTests
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketStateService _stateService;
    private readonly ProcessRejectedPaymentCommandHandler _sut;

    public ProcessRejectedPaymentCommandHandlerTests()
    {
        _ticketRepository = Substitute.For<ITicketRepository>();
        _stateService = Substitute.For<ITicketStateService>();
        var logger = Substitute.For<ILogger<ProcessRejectedPaymentCommandHandler>>();
        _sut = new ProcessRejectedPaymentCommandHandler(_ticketRepository, _stateService, logger);
    }

    private static ProcessRejectedPaymentCommand CreateCommand(long ticketId = 1) => new(
        TicketId: ticketId,
        PaymentId: 10,
        ProviderReference: null,
        RejectionReason: "Insufficient funds",
        RejectedAt: DateTime.UtcNow,
        EventId: 100);

    [Fact]
    public async Task RejectedPayment_TicketNotFound_ReturnsFailure()
    {
        var command = CreateCommand(ticketId: 99);
        _ticketRepository.GetByIdAsync(99).Returns((Ticket?)null);

        var result = await _sut.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.FailureReason!);
    }

    [Fact]
    public async Task RejectedPayment_AlreadyReleased_ReturnsAlreadyProcessed()
    {
        var command = CreateCommand();
        _ticketRepository.GetByIdAsync(1).Returns(new Ticket { Id = 1, Status = TicketStatus.released });

        var result = await _sut.HandleAsync(command);

        Assert.True(result.IsAlreadyProcessed);
    }

    [Fact]
    public async Task RejectedPayment_HappyPath_TransitionsToReleased()
    {
        var command = CreateCommand();
        _ticketRepository.GetByIdAsync(1).Returns(new Ticket { Id = 1, Status = TicketStatus.reserved });
        _stateService.TransitionToReleasedAsync(1, Arg.Any<string>()).Returns(true);

        var result = await _sut.HandleAsync(command);

        Assert.True(result.IsSuccess);
        await _stateService.Received(1).TransitionToReleasedAsync(1,
            Arg.Is<string>(s => s.Contains("Insufficient funds")));
    }

    [Fact]
    public async Task RejectedPayment_TransitionFails_ReturnsFailure()
    {
        var command = CreateCommand();
        _ticketRepository.GetByIdAsync(1).Returns(new Ticket { Id = 1, Status = TicketStatus.reserved });
        _stateService.TransitionToReleasedAsync(1, Arg.Any<string>()).Returns(false);

        var result = await _sut.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to transition", result.FailureReason!);
    }
}
