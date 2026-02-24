using Maliev.DeliveryService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.DeliveryService.Data.Configurations;

internal class DeliveryNoteFileConfiguration : IEntityTypeConfiguration<DeliveryNoteFile>
{
    public void Configure(EntityTypeBuilder<DeliveryNoteFile> builder)
    {
        builder.ToTable("delivery_note_files");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(f => f.DeliveryNoteId).HasColumnName("delivery_note_id").HasMaxLength(50).IsRequired();
        builder.Property(f => f.FileName).HasColumnName("file_name").HasMaxLength(500).IsRequired();
        builder.Property(f => f.StorageUrl).HasColumnName("storage_url").HasMaxLength(2000).IsRequired();
        builder.Property(f => f.ContentType).HasColumnName("content_type").HasMaxLength(200).IsRequired();
        builder.Property(f => f.FileSize).HasColumnName("file_size").IsRequired();
        builder.Property(f => f.FileType).HasColumnName("file_type").HasMaxLength(50).IsRequired()
            .HasConversion<string>();
        builder.Property(f => f.Description).HasColumnName("description");
        builder.Property(f => f.UploadedAt).HasColumnName("uploaded_at").IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(f => f.UploadedBy).HasColumnName("uploaded_by").HasMaxLength(200).IsRequired();
        builder.Property(f => f.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(f => f.DeletedAt).HasColumnName("deleted_at");

        // Index
        builder.HasIndex(f => f.DeliveryNoteId).HasDatabaseName("idx_delivery_note_files_delivery_note_id")
            .HasFilter("NOT is_deleted");
    }
}
