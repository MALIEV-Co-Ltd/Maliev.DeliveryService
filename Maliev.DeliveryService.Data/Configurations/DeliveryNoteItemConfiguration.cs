using Maliev.DeliveryService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.DeliveryService.Data.Configurations;

public class DeliveryNoteItemConfiguration : IEntityTypeConfiguration<DeliveryNoteItem>
{
    public void Configure(EntityTypeBuilder<DeliveryNoteItem> builder)
    {
        builder.ToTable("delivery_note_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(i => i.DeliveryNoteId).HasColumnName("delivery_note_id").HasMaxLength(50).IsRequired();
        builder.Property(i => i.OrderId).HasColumnName("order_id").HasMaxLength(50);
        builder.Property(i => i.PurchaseOrderItemId).HasColumnName("purchase_order_item_id");
        builder.Property(i => i.ProductCode).HasColumnName("product_code").HasMaxLength(100);
        builder.Property(i => i.ProductName).HasColumnName("product_name").HasMaxLength(500);
        builder.Property(i => i.ProductDescription).HasColumnName("product_description");

        builder.Property(i => i.QuantityOrdered).HasColumnName("quantity_ordered")
            .HasColumnType("decimal(18,4)").IsRequired();
        builder.Property(i => i.QuantityManufactured).HasColumnName("quantity_manufactured")
            .HasColumnType("decimal(18,4)").IsRequired();
        builder.Property(i => i.QuantityDelivered).HasColumnName("quantity_delivered")
            .HasColumnType("decimal(18,4)").IsRequired();

        builder.Property(i => i.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(50).IsRequired();
        builder.Property(i => i.ItemNotes).HasColumnName("item_notes");
        builder.Property(i => i.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("NOW()");

        // Check Constraints
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_delivery_note_items_quantity_ordered", "quantity_ordered >= 0");
            t.HasCheckConstraint("CK_delivery_note_items_quantity_manufactured", "quantity_manufactured >= 0");
            t.HasCheckConstraint("CK_delivery_note_items_quantity_delivered_positive", "quantity_delivered > 0");
            t.HasCheckConstraint("CK_delivery_note_items_quantity_delivered_max", "quantity_delivered <= quantity_manufactured");
        });

        // Index
        builder.HasIndex(i => i.DeliveryNoteId).HasDatabaseName("idx_delivery_note_items_delivery_note_id");
    }
}
