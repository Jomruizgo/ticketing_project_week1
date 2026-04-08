namespace CrudService.Infrastructure.Strategies;

using CrudService.Domain.Entities;
using CrudService.Domain.Interfaces;

public class FifoStrategy : IPrioritizationStrategy
{
    public WaitlistEntry? SelectNextEligible(IReadOnlyList<WaitlistEntry> activeEntries)
    {
        return activeEntries
            .OrderBy(e => e.EnrolledAt)
            .FirstOrDefault();
    }
}
