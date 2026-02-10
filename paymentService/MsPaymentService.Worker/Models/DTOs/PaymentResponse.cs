using MsPaymentService.Worker.Models.Entities;

namespace MsPaymentService.Worker.Models.DTOs;

public class PaymentResponse
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ProviderRef { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}