using Maliev.DeliveryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.DeliveryService.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the <see cref="DeliveryNoteItem"/> entity.
/// </summary>
public class DeliveryNoteItemConfiguration : IEntityTypeConfiguration<DeliveryNoteItem>
{
    /// <summary>
    /// Configures the entity of type <see cref="DeliveryNoteItem"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<DeliveryNoteItem> builder)
    {
        builder.ToTable("delivery_note_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").UseIdentityByDefaultColumn();

        builder.Property(i => i.DeliveryNoteId).HasColumnName("delivery_note_id").HasMaxLength(50).IsRequired();
        builder.Property(i => i.ProductCode).HasColumnName("product_code").HasMaxLength(100).IsRequired();
        builder.Property(i => i.ProductName).HasColumnName("product_name").HasMaxLength(500).IsRequired();
        builder.Property(i => i.QuantityOrdered).HasColumnName("quantity_ordered").HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.QuantityManufactured).HasColumnName("quantity_manufactured").HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.QuantityDelivered).HasColumnName("quantity_delivered").HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(20).IsRequired();

        // Relationship
        builder.HasOne(i => i.DeliveryNote)
            .WithMany(dn => dn.Items)
            .HasForeignKey(i => i.DeliveryNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indices
        builder.HasIndex(i => i.DeliveryNoteId).HasDatabaseName("idx_delivery_note_items_dn_id");
        builder.HasIndex(i => i.ProductCode).HasDatabaseName("idx_delivery_note_items_product");
    }
}
