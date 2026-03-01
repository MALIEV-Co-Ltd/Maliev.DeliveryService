using Maliev.DeliveryService.Infrastructure.Persistence;
using Maliev.DeliveryService.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Maliev.DeliveryService.Tests.Integration.TestFixtures;

public class DeliveryServiceTestFixture : BaseIntegrationTestFactory<Program, DeliveryDbContext>
{
    protected override void ConfigureAdditionalServices(IServiceCollection services)
    {
        // Register fake services
        services.AddSingleton<Application.Abstractions.IFileStorageService, Fakes.FakeFileStorageService>();
    }
}

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<DeliveryServiceTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
