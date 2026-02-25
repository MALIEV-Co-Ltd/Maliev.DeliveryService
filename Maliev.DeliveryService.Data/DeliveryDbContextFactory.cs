using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Maliev.DeliveryService.Data;

public class DeliveryDbContextFactory : IDesignTimeDbContextFactory<DeliveryDbContext>
{
    public DeliveryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DeliveryDbContext>();
        
        // Use a dummy connection string for design-time operations
        optionsBuilder.UseNpgsql("Host=localhost;Database=dummy;Username=postgres;Password=postgres");

        return new DeliveryDbContext(optionsBuilder.Options);
    }
}
