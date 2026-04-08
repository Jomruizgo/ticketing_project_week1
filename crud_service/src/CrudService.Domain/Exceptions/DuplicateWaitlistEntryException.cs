namespace CrudService.Domain.Exceptions;

public class DuplicateWaitlistEntryException : Exception
{
    public DuplicateWaitlistEntryException()
        : base("An active waitlist entry already exists for this buyer and event.") { }
}
