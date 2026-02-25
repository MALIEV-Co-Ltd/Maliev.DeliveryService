using Maliev.DeliveryService.Data;
using Maliev.DeliveryService.Tests.Integration.TestFixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.DeliveryService.Tests.Integration;

/// <summary>
/// Base class for integration tests providing common utilities and cleanup helpers.
/// </summary>
[Collection("IntegrationTests")]
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly DeliveryServiceTestFixture Fixture;
    protected readonly HttpClient Client;

    protected BaseIntegrationTest(DeliveryServiceTestFixture fixture)
    {
        Fixture = fixture;
        Client = Fixture.CreateAuthenticatedClient(userId: "test-user-id");
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Clean database after all tests in this class complete
        await Fixture.CleanDatabaseAsync();
    }

    /// <summary>
    /// Cleans the database to ensure test isolation.
    /// Call this at the start of each test method to ensure a clean state.
    /// </summary>
    protected async Task CleanDatabaseAsync()
    {
        await Fixture.CleanDatabaseAsync();
    }
}
