using CrudService.Application.UseCases.Waitlist.EnrollInWaitlist;
using CrudService.Domain.Entities;
using CrudService.Domain.Enums;
using CrudService.Domain.Exceptions;
using CrudService.Domain.Interfaces;
using NSubstitute;

namespace CrudService.Application.Tests.Waitlist;

public class EnrollInWaitlistHandlerTests
{
    private readonly IEventRepository _eventRepository;
    private readonly IWaitlistEntryRepository _waitlistEntryRepository;
    private readonly EnrollInWaitlistHandler _handler;

    public EnrollInWaitlistHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _waitlistEntryRepository = Substitute.For<IWaitlistEntryRepository>();
        _handler = new EnrollInWaitlistHandler(_eventRepository, _waitlistEntryRepository);
    }

    // --- TC-HU1-01: Inscripción exitosa ---

    [Fact(DisplayName = "TC-HU1-01: Valid request creates active waitlist entry")]
    public async Task EnrollInWaitlist_ValidRequest_ReturnsActiveEntry()
    {
        var futureEvent = new Event { Id = 1, Name = "Concert", StartsAt = DateTime.UtcNow.AddDays(30) };
        _eventRepository.GetByIdAsync(1).Returns(futureEvent);
        _waitlistEntryRepository.ExistsActiveAsync(1, "buyer@test.com").Returns(false);
        _waitlistEntryRepository.AddAsync(Arg.Any<WaitlistEntry>())
            .Returns(callInfo =>
            {
                var entry = callInfo.Arg<WaitlistEntry>();
                entry.Id = 10;
                return entry;
            });

        var command = new EnrollInWaitlistCommand(1, "buyer@test.com");
        var result = await _handler.HandleAsync(command);

        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Equal(1, result.EventId);
        Assert.Equal("buyer@test.com", result.BuyerEmail);
        Assert.Equal("active", result.Status);
    }

    // --- TC-HU1-04: Reinscripción tras consumed ---

    [Fact(DisplayName = "TC-HU1-04a: Re-enrollment after consumed entry succeeds")]
    public async Task EnrollInWaitlist_PreviousConsumedEntry_AllowsReenrollment()
    {
        var futureEvent = new Event { Id = 1, Name = "Concert", StartsAt = DateTime.UtcNow.AddDays(30) };
        _eventRepository.GetByIdAsync(1).Returns(futureEvent);
        _waitlistEntryRepository.ExistsActiveAsync(1, "buyer@test.com").Returns(false);
        _waitlistEntryRepository.AddAsync(Arg.Any<WaitlistEntry>())
            .Returns(callInfo =>
            {
                var entry = callInfo.Arg<WaitlistEntry>();
                entry.Id = 11;
                return entry;
            });

        var command = new EnrollInWaitlistCommand(1, "buyer@test.com");
        var result = await _handler.HandleAsync(command);

        Assert.Equal("active", result.Status);
        Assert.Equal(11, result.Id);
    }

    // --- TC-HU1-04: Reinscripción tras expired ---

    [Fact(DisplayName = "TC-HU1-04b: Re-enrollment after expired entry succeeds")]
    public async Task EnrollInWaitlist_PreviousExpiredEntry_AllowsReenrollment()
    {
        var futureEvent = new Event { Id = 1, Name = "Concert", StartsAt = DateTime.UtcNow.AddDays(30) };
        _eventRepository.GetByIdAsync(1).Returns(futureEvent);
        _waitlistEntryRepository.ExistsActiveAsync(1, "buyer@test.com").Returns(false);
        _waitlistEntryRepository.AddAsync(Arg.Any<WaitlistEntry>())
            .Returns(callInfo =>
            {
                var entry = callInfo.Arg<WaitlistEntry>();
                entry.Id = 12;
                return entry;
            });

        var command = new EnrollInWaitlistCommand(1, "buyer@test.com");
        var result = await _handler.HandleAsync(command);

        Assert.Equal("active", result.Status);
        Assert.Equal(12, result.Id);
    }

    // --- TC-HU1-02: Rechazo de duplicado ---

    [Fact(DisplayName = "TC-HU1-02: Duplicate active entry throws DuplicateWaitlistEntryException")]
    public async Task EnrollInWaitlist_ActiveEntryExists_ThrowsDuplicateException()
    {
        var futureEvent = new Event { Id = 1, Name = "Concert", StartsAt = DateTime.UtcNow.AddDays(30) };
        _eventRepository.GetByIdAsync(1).Returns(futureEvent);
        _waitlistEntryRepository.ExistsActiveAsync(1, "buyer@test.com").Returns(true);

        var command = new EnrollInWaitlistCommand(1, "buyer@test.com");

        await Assert.ThrowsAsync<DuplicateWaitlistEntryException>(
            () => _handler.HandleAsync(command));
    }

    [Fact(DisplayName = "TC-HU1-02 edge: Active entry on different event allows enrollment")]
    public async Task EnrollInWaitlist_ActiveEntryDifferentEvent_Succeeds()
    {
        var futureEvent = new Event { Id = 2, Name = "Festival", StartsAt = DateTime.UtcNow.AddDays(30) };
        _eventRepository.GetByIdAsync(2).Returns(futureEvent);
        _waitlistEntryRepository.ExistsActiveAsync(2, "buyer@test.com").Returns(false);
        _waitlistEntryRepository.AddAsync(Arg.Any<WaitlistEntry>())
            .Returns(callInfo =>
            {
                var entry = callInfo.Arg<WaitlistEntry>();
                entry.Id = 13;
                return entry;
            });

        var command = new EnrollInWaitlistCommand(2, "buyer@test.com");
        var result = await _handler.HandleAsync(command);

        Assert.Equal("active", result.Status);
        Assert.Equal(2, result.EventId);
    }

    // --- TC-HU1-03: Lista cerrada ---

    [Fact(DisplayName = "TC-HU1-03: Event date reached throws WaitlistClosedException")]
    public async Task EnrollInWaitlist_EventDateReached_ThrowsWaitlistClosedException()
    {
        var pastEvent = new Event { Id = 1, Name = "Concert", StartsAt = DateTime.UtcNow.AddDays(-1) };
        _eventRepository.GetByIdAsync(1).Returns(pastEvent);

        var command = new EnrollInWaitlistCommand(1, "buyer@test.com");

        await Assert.ThrowsAsync<WaitlistClosedException>(
            () => _handler.HandleAsync(command));
    }

    // --- Edge: Evento no encontrado ---

    [Fact(DisplayName = "Event not found throws EventNotFoundException")]
    public async Task EnrollInWaitlist_EventNotFound_ThrowsEventNotFoundException()
    {
        _eventRepository.GetByIdAsync(999).Returns((Event?)null);

        var command = new EnrollInWaitlistCommand(999, "buyer@test.com");

        await Assert.ThrowsAsync<EventNotFoundException>(
            () => _handler.HandleAsync(command));
    }
}
