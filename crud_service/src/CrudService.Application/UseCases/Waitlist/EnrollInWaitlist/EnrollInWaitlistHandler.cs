namespace CrudService.Application.UseCases.Waitlist.EnrollInWaitlist;

using CrudService.Application.Dtos;
using CrudService.Domain.Entities;
using CrudService.Domain.Enums;
using CrudService.Domain.Exceptions;
using CrudService.Domain.Interfaces;

public class EnrollInWaitlistHandler : IEnrollInWaitlistUseCase
{
    private readonly IEventRepository _eventRepository;
    private readonly IWaitlistEntryRepository _waitlistEntryRepository;

    public EnrollInWaitlistHandler(
        IEventRepository eventRepository,
        IWaitlistEntryRepository waitlistEntryRepository)
    {
        _eventRepository = eventRepository;
        _waitlistEntryRepository = waitlistEntryRepository;
    }

    public async Task<WaitlistEntryDto> HandleAsync(EnrollInWaitlistCommand command)
    {
        var @event = await _eventRepository.GetByIdAsync(command.EventId)
            ?? throw new EventNotFoundException(command.EventId);

        if (@event.StartsAt <= DateTime.UtcNow)
            throw new WaitlistClosedException();

        if (await _waitlistEntryRepository.ExistsActiveAsync(command.EventId, command.BuyerEmail))
            throw new DuplicateWaitlistEntryException();

        var entry = new WaitlistEntry
        {
            EventId = command.EventId,
            BuyerEmail = command.BuyerEmail,
            Status = WaitlistEntryStatus.Active,
            EnrolledAt = DateTime.UtcNow
        };

        var created = await _waitlistEntryRepository.AddAsync(entry);

        return new WaitlistEntryDto
        {
            Id = created.Id,
            EventId = created.EventId,
            BuyerEmail = created.BuyerEmail,
            Status = created.Status.ToString().ToLowerInvariant(),
            EnrolledAt = created.EnrolledAt
        };
    }
}
