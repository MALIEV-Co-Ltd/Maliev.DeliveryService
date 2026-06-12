using Maliev.DeliveryService.Infrastructure.Persistence.Configurations;
using Maliev.DeliveryService.Domain.Entities;
using Maliev.Aspire.ServiceDefaults.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Maliev.DeliveryService.Infrastructure.Persistence;

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

    /// <summary>
    /// Gets or sets the Delivery Notes collection.
    /// </summary>
    public DbSet<DeliveryNote> DeliveryNotes => Set<DeliveryNote>();

    /// <summary>
    /// Gets or sets the Delivery Note Items collection.
    /// </summary>
    public DbSet<DeliveryNoteItem> DeliveryNoteItems => Set<DeliveryNoteItem>();

    /// <summary>
    /// Gets or sets the Delivery Note Files collection.
    /// </summary>
    public DbSet<DeliveryNoteFile> DeliveryNoteFiles => Set<DeliveryNoteFile>();

    /// <summary>
    /// Gets or sets the Addresses collection.
    /// </summary>
    public DbSet<Address> Addresses => Set<Address>();

    /// <summary>
    /// Configures the model that was discovered by convention from the entity types.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new DeliveryNoteConfiguration());
        modelBuilder.ApplyConfiguration(new DeliveryNoteItemConfiguration());
        modelBuilder.ApplyConfiguration(new DeliveryNoteFileConfiguration());
        modelBuilder.ApplyConfiguration(new AddressConfiguration());

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

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
