using Maliev.DeliveryService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.DeliveryService.Data.Configurations;

internal class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("addresses");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.CompanyName).HasColumnName("company_name").HasMaxLength(500);
        builder.Property(a => a.ContactName).HasColumnName("contact_name").HasMaxLength(200);
        builder.Property(a => a.AddressLine1).HasColumnName("address_line1").HasMaxLength(500).IsRequired();
        builder.Property(a => a.AddressLine2).HasColumnName("address_line2").HasMaxLength(500);
        builder.Property(a => a.City).HasColumnName("city").HasMaxLength(200).IsRequired();
        builder.Property(a => a.StateProvince).HasColumnName("state_province").HasMaxLength(200).IsRequired();
        builder.Property(a => a.PostalCode).HasColumnName("postal_code").HasMaxLength(20).IsRequired();
        builder.Property(a => a.Country).HasColumnName("country").HasMaxLength(100).IsRequired();
        builder.Property(a => a.PhoneNumber).HasColumnName("phone_number").HasMaxLength(50);
        builder.Property(a => a.EmailAddress).HasColumnName("email_address").HasMaxLength(200);

        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");
        builder.Property(a => a.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200);
    }
}
