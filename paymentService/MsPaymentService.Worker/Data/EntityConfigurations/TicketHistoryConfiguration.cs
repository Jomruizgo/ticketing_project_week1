using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MsPaymentService.Worker.Models.Entities;

namespace MsPaymentService.Worker.Data.EntityConfigurations;

public class TicketHistoryConfiguration : IEntityTypeConfiguration<TicketHistory>
{
    public void Configure(EntityTypeBuilder<TicketHistory> builder)
    {
        builder.ToTable("ticket_history");
        
        builder.HasKey(h => h.Id);
        
        builder.Property(h => h.Id)
            .HasColumnName("id");
            
        builder.Property(h => h.TicketId)
            .HasColumnName("ticket_id");
        
        builder.Property(h => h.OldStatus)
            .HasColumnName("old_status")
            .HasConversion<string>()
            .IsRequired();
            
        builder.Property(h => h.NewStatus)
            .HasColumnName("new_status")
            .HasConversion<string>()
            .IsRequired();
            
        builder.Property(h => h.ChangedAt)
            .HasColumnName("changed_at")
            .HasDefaultValueSql("NOW()");
            
        builder.Property(h => h.Reason)
            .HasColumnName("reason")
            .HasMaxLength(200);
        
        // Relaciones
        builder.HasOne(h => h.Ticket)
            .WithMany(t => t.History)
            .HasForeignKey(h => h.TicketId);
    }
}