namespace CrudService.Domain.Entities;

public enum PaymentStatus
{
    Pending,
    Approved,
    Failed,
    Expired
}

public class Payment
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? ProviderRef { get; set; }
    public int AmountCents { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Ticket Ticket { get; set; } = null!;
}
