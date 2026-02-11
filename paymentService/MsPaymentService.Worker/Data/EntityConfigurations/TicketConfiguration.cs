using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MsPaymentService.Worker.Models.Entities;

namespace MsPaymentService.Worker.Data.EntityConfigurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");
        
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.Id)
            .HasColumnName("id");
            
        builder.Property(t => t.EventId)
            .HasColumnName("event_id");
        
        builder.Property(t => t.Status)
            .HasColumnName("status")
            .IsRequired();
        
        builder.Property(t => t.ReservedAt)
            .HasColumnName("reserved_at");
        
        builder.Property(t => t.ExpiresAt)
            .HasColumnName("expires_at");
        
        builder.Property(t => t.PaidAt)
            .HasColumnName("paid_at");
            
        builder.Property(t => t.OrderId)
            .HasColumnName("order_id")
            .HasMaxLength(80);
            
        builder.Property(t => t.ReservedBy)
            .HasColumnName("reserved_by")
            .HasMaxLength(120);
        
        builder.Property(t => t.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();
        
        // Relaciones
        builder.HasMany(t => t.Payments)
            .WithOne(p => p.Ticket)
            .HasForeignKey(p => p.TicketId);
        
        builder.HasMany(t => t.History)
            .WithOne(h => h.Ticket)
            .HasForeignKey(h => h.TicketId);
        
        // Índices
        builder.HasIndex(t => new { t.Status, t.ExpiresAt })
            .HasDatabaseName("idx_tickets_status_expires_at");
            
        builder.HasIndex(t => t.EventId)
            .HasDatabaseName("idx_tickets_event_id");
    }
}