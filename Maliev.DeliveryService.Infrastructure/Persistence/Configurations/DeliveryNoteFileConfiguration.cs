using Maliev.DeliveryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.DeliveryService.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the <see cref="DeliveryNoteFile"/> entity.
/// </summary>
public class DeliveryNoteFileConfiguration : IEntityTypeConfiguration<DeliveryNoteFile>
{
    /// <summary>
    /// Configures the entity of type <see cref="DeliveryNoteFile"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<DeliveryNoteFile> builder)
    {
        builder.ToTable("delivery_note_files");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(f => f.DeliveryNoteId).HasColumnName("delivery_note_id").HasMaxLength(50).IsRequired();
        builder.Property(f => f.FileName).HasColumnName("file_name").HasMaxLength(500).IsRequired();
        builder.Property(f => f.StorageUrl).HasColumnName("storage_url").HasMaxLength(1000).IsRequired();
        builder.Property(f => f.ContentType).HasColumnName("content_type").HasMaxLength(100).IsRequired();
        builder.Property(f => f.FileSize).HasColumnName("file_size").IsRequired();
        builder.Property(f => f.FileType).HasColumnName("file_type").IsRequired().HasConversion<string>();
        builder.Property(f => f.Description).HasColumnName("description").HasMaxLength(500);

        builder.Property(f => f.UploadedAt).HasColumnName("uploaded_at").IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(f => f.UploadedBy).HasColumnName("uploaded_by").HasMaxLength(200).IsRequired();

        builder.Property(f => f.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
        builder.Property(f => f.DeletedAt).HasColumnName("deleted_at");

        // Relationship
        builder.HasOne(f => f.DeliveryNote)
            .WithMany(dn => dn.Files)
            .HasForeignKey(f => f.DeliveryNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indices
        builder.HasIndex(f => f.DeliveryNoteId).HasDatabaseName("idx_delivery_note_files_dn_id");

        // Global query filter for soft delete
        builder.HasQueryFilter(f => !f.IsDeleted);
    }
}
