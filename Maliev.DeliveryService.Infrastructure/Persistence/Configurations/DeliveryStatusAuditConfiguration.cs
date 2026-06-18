using Maliev.DeliveryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.DeliveryService.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the <see cref="DeliveryStatusAudit"/> entity.
/// </summary>
public class DeliveryStatusAuditConfiguration : IEntityTypeConfiguration<DeliveryStatusAudit>
{
    /// <summary>
    /// Configures the entity of type <see cref="DeliveryStatusAudit"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<DeliveryStatusAudit> builder)
    {
        builder.ToTable("delivery_status_audits");

        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.Id).HasColumnName("id").IsRequired();
        builder.Property(audit => audit.DeliveryNoteId).HasColumnName("delivery_note_id").HasMaxLength(50).IsRequired();
        builder.Property(audit => audit.PreviousStatus).HasColumnName("previous_status").HasConversion<string>().IsRequired();
        builder.Property(audit => audit.NewStatus).HasColumnName("new_status").HasConversion<string>().IsRequired();
        builder.Property(audit => audit.ChangedBy).HasColumnName("changed_by").HasMaxLength(200).IsRequired();
        builder.Property(audit => audit.ChangedAt).HasColumnName("changed_at").IsRequired();

        builder.HasOne(audit => audit.DeliveryNote)
            .WithMany()
            .HasForeignKey(audit => audit.DeliveryNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(audit => audit.DeliveryNoteId)
            .HasDatabaseName("idx_delivery_status_audits_delivery_note_id");
        builder.HasIndex(audit => audit.ChangedAt)
            .HasDatabaseName("idx_delivery_status_audits_changed_at");
    }
}
