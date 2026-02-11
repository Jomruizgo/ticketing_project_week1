using Microsoft.EntityFrameworkCore;
using ReservationService.Worker.Models;

namespace ReservationService.Worker.Data;

public class TicketingDbContext : DbContext
{
    public TicketingDbContext(DbContextOptions<TicketingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("tickets");

            entity.HasKey(t => t.Id);

            entity.Property(t => t.Id).HasColumnName("id");
            entity.Property(t => t.EventId).HasColumnName("event_id");
            entity.Property(t => t.SectionId).HasColumnName("section_id");
            entity.Property(t => t.ReservedAt).HasColumnName("reserved_at");
            entity.Property(t => t.ExpiresAt).HasColumnName("expires_at");
            entity.Property(t => t.PaidAt).HasColumnName("paid_at");
            entity.Property(t => t.OrderId).HasColumnName("order_id").HasMaxLength(80);
            entity.Property(t => t.ReservedBy).HasColumnName("reserved_by").HasMaxLength(120);
            entity.Property(t => t.Version).HasColumnName("version");

            // Mapeo del enum a string para PostgreSQL
            entity.Property(t => t.Status)
                .HasColumnName("status")
                .HasConversion<string>();
        });
    }
}
