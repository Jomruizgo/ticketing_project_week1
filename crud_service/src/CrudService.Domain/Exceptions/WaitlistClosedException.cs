namespace CrudService.Domain.Exceptions;

public class WaitlistClosedException : Exception
{
    public WaitlistClosedException()
        : base("The waitlist for this event is closed.") { }
}
