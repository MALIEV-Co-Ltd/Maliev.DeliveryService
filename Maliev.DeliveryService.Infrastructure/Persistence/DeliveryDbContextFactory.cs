using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.DeliveryService.Infrastructure.Persistence;

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
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DeliveryDbContext")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DeliveryDb")
            ?? throw new InvalidOperationException(
                "Set ConnectionStrings__DeliveryDbContext for design-time EF operations.");

        optionsBuilder.UseNpgsql(connectionString);

        return new DeliveryDbContext(optionsBuilder.Options);
    }
}
