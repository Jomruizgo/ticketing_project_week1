using CrudService.Domain.Entities;
using CrudService.Domain.Enums;
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
    public DbSet<WaitlistEntry> WaitlistEntries { get; set; } = null!;
    public DbSet<WaitlistOpportunity> WaitlistOpportunities { get; set; } = null!;
    public DbSet<NotificationDelivery> NotificationDeliveries { get; set; } = null!;

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
        modelBuilder.HasPostgresEnum<WaitlistEntryStatus>("waitlist_entry_status");
        modelBuilder.HasPostgresEnum<WaitlistOpportunityStatus>("waitlist_opportunity_status");
        modelBuilder.HasPostgresEnum<NotificationDeliveryStatus>("notification_delivery_status");

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

        modelBuilder.Entity<WaitlistEntry>()
            .HasKey(w => w.Id);
        modelBuilder.Entity<WaitlistEntry>()
            .Property(w => w.BuyerEmail)
            .HasMaxLength(255)
            .IsRequired();
        modelBuilder.Entity<WaitlistEntry>()
            .Property(w => w.Status)
            .HasColumnType("waitlist_entry_status");
        modelBuilder.Entity<WaitlistEntry>()
            .HasOne(w => w.Event)
            .WithMany()
            .HasForeignKey(w => w.EventId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WaitlistEntry>()
            .HasIndex(w => new { w.EventId, w.BuyerEmail })
            .IsUnique()
            .HasFilter("status = 'active'")
            .HasDatabaseName("idx_waitlist_entries_active_unique");

        modelBuilder.Entity<WaitlistOpportunity>()
            .HasKey(o => o.Id);
        modelBuilder.Entity<WaitlistOpportunity>()
            .Property(o => o.Status)
            .HasColumnType("waitlist_opportunity_status");
        modelBuilder.Entity<WaitlistOpportunity>()
            .HasOne(o => o.WaitlistEntry)
            .WithMany()
            .HasForeignKey(o => o.WaitlistEntryId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WaitlistOpportunity>()
            .HasOne(o => o.Ticket)
            .WithMany()
            .HasForeignKey(o => o.TicketId)
            .OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<WaitlistOpportunity>()
            .HasIndex(o => o.WaitlistEntryId)
            .HasDatabaseName("idx_waitlist_opportunities_entry");
        modelBuilder.Entity<WaitlistOpportunity>()
            .HasIndex(o => o.TicketId)
            .IsUnique()
            .HasFilter("status = 'active'")
            .HasDatabaseName("idx_waitlist_opportunities_active_ticket_unique");
        modelBuilder.Entity<WaitlistOpportunity>()
            .HasIndex(o => o.WaitlistEntryId)
            .IsUnique()
            .HasFilter("status = 'active'")
            .HasDatabaseName("idx_waitlist_opportunities_active_entry_unique");

        modelBuilder.Entity<NotificationDelivery>()
            .HasKey(n => n.Id);
        modelBuilder.Entity<NotificationDelivery>()
            .Property(n => n.Status)
            .HasColumnType("notification_delivery_status");
        modelBuilder.Entity<NotificationDelivery>()
            .Property(n => n.Channel)
            .HasMaxLength(50)
            .IsRequired();
        modelBuilder.Entity<NotificationDelivery>()
            .HasOne(n => n.WaitlistOpportunity)
            .WithMany()
            .HasForeignKey(n => n.WaitlistOpportunityId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<NotificationDelivery>()
            .HasIndex(n => n.WaitlistOpportunityId)
            .HasDatabaseName("idx_notification_deliveries_opportunity");
    }
}
