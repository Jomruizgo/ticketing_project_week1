namespace CrudService.Domain.Exceptions;

using CrudService.Domain.Enums;

public class InvalidOpportunityTransitionException : Exception
{
    public InvalidOpportunityTransitionException(
        WaitlistOpportunityStatus currentStatus,
        WaitlistOpportunityStatus targetStatus)
        : base($"Invalid opportunity transition from {currentStatus} to {targetStatus}.")
    {
    }
}
