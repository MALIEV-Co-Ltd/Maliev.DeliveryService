using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.DeliveryService.Data;

/// <summary>
/// Factory for creating the <see cref="DeliveryDbContext"/> at design time.
/// </summary>
public class DeliveryDbContextFactory : IDesignTimeDbContextFactory<DeliveryDbContext>
{
    /// <summary>
    /// Creates a new instance of the <see cref="DeliveryDbContext"/>.
    /// </summary>
    /// <param name="args">Arguments provided by the design-time tool.</param>
    /// <returns>An instance of the <see cref="DeliveryDbContext"/>.</returns>
    public DeliveryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DeliveryDbContext>();
        
        // Use a dummy connection string for design-time operations
        optionsBuilder.UseNpgsql("Host=localhost;Database=dummy;Username=postgres;Password=postgres");

        return new DeliveryDbContext(optionsBuilder.Options);
    }
}
