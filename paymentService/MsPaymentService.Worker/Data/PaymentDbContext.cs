using Microsoft.EntityFrameworkCore;
using MsPaymentService.Worker.Models.Entities;

namespace MsPaymentService.Worker.Data;

public class PaymentDbContext : DbContext
{
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<TicketHistory> TicketHistory { get; set; }
    public DbSet<Event> Events { get; set; }

    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);
        
        // Configurar enums como PostgreSQL enums
        modelBuilder.HasPostgresEnum<TicketStatus>();
        modelBuilder.HasPostgresEnum<PaymentStatus>();
    }
}