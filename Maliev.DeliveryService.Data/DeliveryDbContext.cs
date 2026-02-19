using Maliev.DeliveryService.Data.Configurations;
using Maliev.DeliveryService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maliev.DeliveryService.Data;

public class DeliveryDbContext : DbContext
{
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
    }
}
