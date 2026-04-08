namespace CrudService.Application.Dtos;

public record EnrollInWaitlistRequest(long EventId, string BuyerEmail);

public class WaitlistEntryDto
{
    public long Id { get; set; }
    public long EventId { get; set; }
    public string BuyerEmail { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime EnrolledAt { get; set; }
}

public class WaitlistOpportunityDto
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int RemainingMinutes { get; set; }
}

public class WaitlistStatusResponse
{
    public WaitlistEntryDto Entry { get; set; } = null!;
    public WaitlistOpportunityDto? Opportunity { get; set; }
}

public record ClaimOpportunityRequest(string BuyerEmail);

public class ClaimOpportunityResponse
{
    public long OpportunityId { get; set; }
    public long TicketId { get; set; }
    public long EventId { get; set; }
    public string Status { get; set; } = "consumed";
}
