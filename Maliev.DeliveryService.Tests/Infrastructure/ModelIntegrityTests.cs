using Maliev.DeliveryService.Infrastructure.Persistence;
using Maliev.DeliveryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maliev.DeliveryService.Tests.Infrastructure;

public class ModelIntegrityTests
{
    [Fact]
    public void Model_ShouldIncludeMassTransitOutboxEntities()
    {
        var options = new DbContextOptionsBuilder<DeliveryDbContext>()
            .UseNpgsql("Host=localhost;Database=ModelCheck")
            .Options;

        using var context = new DeliveryDbContext(options);
        var entityNames = context.Model.GetEntityTypes()
            .Select(entity => entity.ClrType.FullName)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("MassTransit.EntityFrameworkCoreIntegration.InboxState", entityNames);
        Assert.Contains("MassTransit.EntityFrameworkCoreIntegration.OutboxMessage", entityNames);
        Assert.Contains("MassTransit.EntityFrameworkCoreIntegration.OutboxState", entityNames);
    }

    [Fact]
    public void Model_ShouldEnforceOneActiveDeliveryNotePerOrder()
    {
        var options = new DbContextOptionsBuilder<DeliveryDbContext>()
            .UseNpgsql("Host=localhost;Database=ModelCheck")
            .Options;

        using var context = new DeliveryDbContext(options);
        var entity = context.Model.FindEntityType(typeof(DeliveryNote));
        Assert.NotNull(entity);

        var index = entity.GetIndexes()
            .SingleOrDefault(index => index.GetDatabaseName() == "ux_delivery_notes_active_order_id");

        Assert.NotNull(index);
        Assert.True(index.IsUnique);
        Assert.Equal("order_id IS NOT NULL AND is_deleted = false", index.GetFilter());
    }
}
