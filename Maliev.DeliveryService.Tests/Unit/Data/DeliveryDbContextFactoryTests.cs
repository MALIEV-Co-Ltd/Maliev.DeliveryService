using Maliev.DeliveryService.Infrastructure.Persistence;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.Data;

public class DeliveryDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_ReturnsContext()
    {
        // Arrange
        var previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DeliveryDbContext");
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DeliveryDbContext",
            "Host=localhost;Database=delivery_test;Username=test;Password=test");
        var factory = new DeliveryDbContextFactory();

        try
        {
            // Act
            var context = factory.CreateDbContext(Array.Empty<string>());

            // Assert
            Assert.NotNull(context);
            Assert.NotNull(context.Database);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DeliveryDbContext", previousConnectionString);
        }
    }

    [Fact]
    public void CreateDbContext_MissingConnectionString_Throws()
    {
        // Arrange
        var previousPrimaryConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DeliveryDbContext");
        var previousLegacyConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DeliveryDb");
        Environment.SetEnvironmentVariable("ConnectionStrings__DeliveryDbContext", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__DeliveryDb", null);
        var factory = new DeliveryDbContextFactory();

        try
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => factory.CreateDbContext(Array.Empty<string>()));
            Assert.Contains("ConnectionStrings__DeliveryDbContext", exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DeliveryDbContext", previousPrimaryConnectionString);
            Environment.SetEnvironmentVariable("ConnectionStrings__DeliveryDb", previousLegacyConnectionString);
        }
    }
}
