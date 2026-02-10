using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MsPaymentService.Worker.Models.Entities;

namespace MsPaymentService.Worker.Data.EntityConfigurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id)
            .HasColumnName("id");
            
        builder.Property(p => p.TicketId)
            .HasColumnName("ticket_id");
        
        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();
            
        builder.Property(p => p.ProviderRef)
            .HasColumnName("provider_ref")
            .HasMaxLength(120);
            
        builder.Property(p => p.AmountCents)
            .HasColumnName("amount_cents");
            
        builder.Property(p => p.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .HasDefaultValue("USD");
            
        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");
            
        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");
        
        // Relaciones
        builder.HasOne(p => p.Ticket)
            .WithMany(t => t.Payments)
            .HasForeignKey(p => p.TicketId);
        
        // Índices
        builder.HasIndex(p => p.TicketId)
            .HasDatabaseName("idx_payments_ticket_id");
            
        builder.HasIndex(p => p.Status)
            .HasDatabaseName("idx_payments_status");
    }
}