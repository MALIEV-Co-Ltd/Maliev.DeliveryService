using Maliev.DeliveryService.Infrastructure.Persistence;
using Xunit;

namespace Maliev.DeliveryService.Tests.Unit.Data;

public class DeliveryDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_ReturnsContext()
    {
        // Arrange
        var factory = new DeliveryDbContextFactory();

        // Act
        var context = factory.CreateDbContext(Array.Empty<string>());

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.Database);
    }
}
