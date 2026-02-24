using Maliev.DeliveryService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.DeliveryService.Data.Configurations;

internal class DeliveryNoteConfiguration : IEntityTypeConfiguration<DeliveryNote>
{
    public void Configure(EntityTypeBuilder<DeliveryNote> builder)
    {
        builder.ToTable("delivery_notes");

        builder.HasKey(dn => dn.DeliveryNoteId);
        builder.Property(dn => dn.DeliveryNoteId).HasColumnName("delivery_note_id").HasMaxLength(50).IsRequired();

        builder.Property(dn => dn.OrderId).HasColumnName("order_id").HasMaxLength(50);
        builder.Property(dn => dn.PurchaseOrderId).HasColumnName("purchase_order_id");
        builder.Property(dn => dn.CustomerId).HasColumnName("customer_id").IsRequired();
        builder.Property(dn => dn.CustomerName).HasColumnName("customer_name").HasMaxLength(500);

        builder.Property(dn => dn.DeliveryDate).HasColumnName("delivery_date").IsRequired();
        builder.Property(dn => dn.ActualDeliveryTime).HasColumnName("actual_delivery_time");
        builder.Property(dn => dn.Status).HasColumnName("status").HasMaxLength(50).IsRequired()
            .HasConversion<string>();

        // Shipping Address
        builder.Property(dn => dn.ShippingAddressId).HasColumnName("shipping_address_id");
        builder.Property(dn => dn.ShippingAddressLine1).HasColumnName("shipping_address_line1").HasMaxLength(500);
        builder.Property(dn => dn.ShippingAddressLine2).HasColumnName("shipping_address_line2").HasMaxLength(500);
        builder.Property(dn => dn.ShippingCity).HasColumnName("shipping_city").HasMaxLength(200);
        builder.Property(dn => dn.ShippingProvince).HasColumnName("shipping_province").HasMaxLength(200);
        builder.Property(dn => dn.ShippingPostalCode).HasColumnName("shipping_postal_code").HasMaxLength(20);
        builder.Property(dn => dn.ShippingCountry).HasColumnName("shipping_country").HasMaxLength(100);

        // Delivery Contact
        builder.Property(dn => dn.DeliveryContactName).HasColumnName("delivery_contact_name").HasMaxLength(200);
        builder.Property(dn => dn.DeliveryContactPhone).HasColumnName("delivery_contact_phone").HasMaxLength(50);
        builder.Property(dn => dn.DeliveryContactEmail).HasColumnName("delivery_contact_email").HasMaxLength(200);

        // Carrier Information
        builder.Property(dn => dn.CarrierName).HasColumnName("carrier_name").HasMaxLength(200);
        builder.Property(dn => dn.TrackingNumber).HasColumnName("tracking_number").HasMaxLength(200);
        builder.Property(dn => dn.ShippingCost).HasColumnName("shipping_cost").HasColumnType("decimal(18,2)");
        builder.Property(dn => dn.ShippingCostCurrency).HasColumnName("shipping_cost_currency").HasMaxLength(10);

        // Delivery Confirmation
        builder.Property(dn => dn.ReceivedByName).HasColumnName("received_by_name").HasMaxLength(200);
        builder.Property(dn => dn.SignatureFileId).HasColumnName("signature_file_id");
        builder.Property(dn => dn.SignedAt).HasColumnName("signed_at");

        // Notes
        builder.Property(dn => dn.InternalNotes).HasColumnName("internal_notes");
        builder.Property(dn => dn.DeliveryInstructions).HasColumnName("delivery_instructions");

        // Audit Fields
        builder.Property(dn => dn.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(dn => dn.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        builder.Property(dn => dn.UpdatedAt).HasColumnName("updated_at");
        builder.Property(dn => dn.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200);

        // Optimistic Concurrency
        builder.Property(dn => dn.RowVersion).HasColumnName("row_version").IsRequired()
            .HasColumnType("integer")
            .HasDefaultValue(0)
            .IsConcurrencyToken();

        // Soft Delete
        builder.Property(dn => dn.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(dn => dn.DeletedAt).HasColumnName("deleted_at");
        builder.Property(dn => dn.DeletedBy).HasColumnName("deleted_by").HasMaxLength(200);

        // Query Filter for Soft Delete
        builder.HasQueryFilter(dn => !dn.IsDeleted);

        // Relationships
        builder.HasMany(dn => dn.Items)
            .WithOne(i => i.DeliveryNote)
            .HasForeignKey(i => i.DeliveryNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(dn => dn.Files)
            .WithOne(f => f.DeliveryNote)
            .HasForeignKey(f => f.DeliveryNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(dn => dn.OrderId).HasDatabaseName("idx_delivery_notes_order_id")
            .HasFilter("NOT is_deleted");
        builder.HasIndex(dn => dn.CustomerId).HasDatabaseName("idx_delivery_notes_customer_id")
            .HasFilter("NOT is_deleted");
        builder.HasIndex(dn => dn.Status).HasDatabaseName("idx_delivery_notes_status")
            .HasFilter("NOT is_deleted");
        builder.HasIndex(dn => dn.DeliveryDate).HasDatabaseName("idx_delivery_notes_delivery_date")
            .HasFilter("NOT is_deleted");
        builder.HasIndex(dn => dn.TrackingNumber).HasDatabaseName("idx_delivery_notes_tracking_number")
            .HasFilter("NOT is_deleted");

        // Composite index for customer + date range queries (most common search pattern)
        builder.HasIndex(dn => new { dn.CustomerId, dn.DeliveryDate })
            .HasDatabaseName("idx_delivery_notes_customer_delivery_date")
            .IsDescending(false, true) // customer_id ASC, delivery_date DESC
            .HasFilter("NOT is_deleted");
    }
}
