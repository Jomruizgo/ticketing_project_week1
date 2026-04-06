using CrudService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrudService.Infrastructure.Persistence;

public class TicketingDbContext : DbContext
{
    public TicketingDbContext(DbContextOptions<TicketingDbContext> options)
        : base(options) { }

    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<Ticket> Tickets { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<TicketHistory> TicketHistories { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<TicketStatus>("ticket_status");
        modelBuilder.HasPostgresEnum<PaymentStatus>("payment_status");

        modelBuilder.Entity<Event>()
            .HasKey(e => e.Id);
        modelBuilder.Entity<Event>()
            .Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        modelBuilder.Entity<Ticket>()
            .HasKey(t => t.Id);
        modelBuilder.Entity<Ticket>()
            .Property(t => t.Status)
            .HasColumnType("ticket_status");
        modelBuilder.Entity<Ticket>()
            .Property(t => t.OrderId)
            .HasMaxLength(80);
        modelBuilder.Entity<Ticket>()
            .Property(t => t.ReservedBy)
            .HasMaxLength(120);
        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Event)
            .WithMany(e => e.Tickets)
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => new { t.Status, t.ExpiresAt })
            .HasDatabaseName("idx_tickets_status_expires_at");
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.EventId)
            .HasDatabaseName("idx_tickets_event_id");

        modelBuilder.Entity<Payment>()
            .HasKey(p => p.Id);
        modelBuilder.Entity<Payment>()
            .Property(p => p.Status)
            .HasColumnType("payment_status");
        modelBuilder.Entity<Payment>()
            .Property(p => p.ProviderRef)
            .HasMaxLength(120);
        modelBuilder.Entity<Payment>()
            .Property(p => p.Currency)
            .HasMaxLength(3)
            .IsRequired();
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Ticket)
            .WithMany(t => t.Payments)
            .HasForeignKey(p => p.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.TicketId)
            .HasDatabaseName("idx_payments_ticket_id");
        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.Status)
            .HasDatabaseName("idx_payments_status");

        modelBuilder.Entity<TicketHistory>()
            .ToTable("ticket_history")
            .HasKey(h => h.Id);
        modelBuilder.Entity<TicketHistory>()
            .Property(h => h.OldStatus)
            .HasColumnType("ticket_status");
        modelBuilder.Entity<TicketHistory>()
            .Property(h => h.NewStatus)
            .HasColumnType("ticket_status");
        modelBuilder.Entity<TicketHistory>()
            .Property(h => h.Reason)
            .HasMaxLength(200);
        modelBuilder.Entity<TicketHistory>()
            .HasOne(h => h.Ticket)
            .WithMany(t => t.History)
            .HasForeignKey(h => h.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
