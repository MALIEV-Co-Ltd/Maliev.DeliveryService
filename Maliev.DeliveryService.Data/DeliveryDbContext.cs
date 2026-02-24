using Maliev.DeliveryService.Data.Configurations;
using Maliev.DeliveryService.Data.Entities;
using Maliev.Aspire.ServiceDefaults.Database;
using Microsoft.EntityFrameworkCore;

namespace Maliev.DeliveryService.Data;

/// <summary>
/// Database context for the Delivery Service.
/// </summary>
public class DeliveryDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryDbContext"/> class.
    /// </summary>
    /// <param name="options">The options for the context.</param>
    public DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : base(options)
    {
    }

    public DbSet<DeliveryNote> DeliveryNotes => Set<DeliveryNote>();
    public DbSet<DeliveryNoteItem> DeliveryNoteItems => Set<DeliveryNoteItem>();
    public DbSet<DeliveryNoteFile> DeliveryNoteFiles => Set<DeliveryNoteFile>();
    public DbSet<Address> Addresses => Set<Address>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new DeliveryNoteConfiguration());
        modelBuilder.ApplyConfiguration(new DeliveryNoteItemConfiguration());
        modelBuilder.ApplyConfiguration(new DeliveryNoteFileConfiguration());
        modelBuilder.ApplyConfiguration(new AddressConfiguration());

        // Apply PostgreSQL snake_case naming convention globally
        SnakeCaseNamingHelper.ApplySnakeCaseNaming(modelBuilder);

        // Global converter for UTC DateTime to prevent Npgsql.UnspecifiedKind exceptions
        var dateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
            }
        }
    }
}
