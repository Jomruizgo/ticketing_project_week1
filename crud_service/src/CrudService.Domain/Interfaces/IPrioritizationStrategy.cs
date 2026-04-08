namespace CrudService.Domain.Interfaces;

using CrudService.Domain.Entities;

public interface IPrioritizationStrategy
{
    WaitlistEntry? SelectNextEligible(IReadOnlyList<WaitlistEntry> activeEntries);
}
