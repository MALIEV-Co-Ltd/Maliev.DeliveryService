using Maliev.DeliveryService.Data.Configurations;
using Maliev.DeliveryService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maliev.DeliveryService.Data;

/// <summary>Database context for the Delivery Service.</summary>
public class DeliveryDbContext : DbContext
{
    /// <summary>Initializes a new instance of the <see cref="DeliveryDbContext"/> class.</summary>
    /// <param name="options">The options to be used by the context.</param>
    public DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : base(options)
    {
    }

    /// <summary>Gets or sets the delivery notes.</summary>
    public DbSet<DeliveryNote> DeliveryNotes => Set<DeliveryNote>();
    /// <summary>Gets or sets the delivery note items.</summary>
    public DbSet<DeliveryNoteItem> DeliveryNoteItems => Set<DeliveryNoteItem>();
    /// <summary>Gets or sets the delivery note files.</summary>
    public DbSet<DeliveryNoteFile> DeliveryNoteFiles => Set<DeliveryNoteFile>();
    /// <summary>Gets or sets the addresses.</summary>
    public DbSet<Address> Addresses => Set<Address>();

    /// <summary>Configures the model using the provided builder.</summary>
    /// <param name="modelBuilder">The builder used to construct the model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new DeliveryNoteConfiguration());
        modelBuilder.ApplyConfiguration(new DeliveryNoteItemConfiguration());
        modelBuilder.ApplyConfiguration(new DeliveryNoteFileConfiguration());
        modelBuilder.ApplyConfiguration(new AddressConfiguration());
    }
}
