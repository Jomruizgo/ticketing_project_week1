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
