using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.DeliveryService.Data;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace Maliev.DeliveryService.Tests.Integration.TestFixtures;

public class DeliveryServiceTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static PostgreSqlContainer? _postgresContainer;
    private static RabbitMqContainer? _rabbitMqContainer;
    private static bool _containersStarted;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    public DeliveryServiceTestFixture()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    public async Task InitializeAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (!_containersStarted)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                _postgresContainer = new PostgreSqlBuilder()
                    .WithImage("postgres:18")
                    .Build();

                _rabbitMqContainer = new RabbitMqBuilder()
                    .WithImage("rabbitmq:4.0-alpine")
                    .Build();
#pragma warning restore CS0618 // Type or member is obsolete

                await Task.WhenAll(
                    _postgresContainer.StartAsync(),
                    _rabbitMqContainer.StartAsync());

                // Run migrations
                var optionsBuilder = new DbContextOptionsBuilder<DeliveryDbContext>();
                optionsBuilder.UseNpgsql(_postgresContainer.GetConnectionString());
                await using var context = new DeliveryDbContext(optionsBuilder.Options);
                await context.Database.MigrateAsync();

                _containersStarted = true;
            }
        }
        finally
        {
            _initLock.Release();
        }

        // Set environment variables so Program.cs picks them up via WebApplication.CreateBuilder
        Environment.SetEnvironmentVariable("ConnectionStrings__DeliveryDbContext", _postgresContainer!.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__rabbitmq", _rabbitMqContainer!.GetConnectionString());
    }

    public new Task DisposeAsync()
    {
        // Static containers are not disposed here (reused across tests)
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        return Task.CompletedTask;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        if (!_containersStarted)
            InitializeAsync().GetAwaiter().GetResult();
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Use UseSetting to inject connection strings into the WebHost configuration
        // that Program.cs reads when calling WebApplication.CreateBuilder
        builder.UseSetting("ConnectionStrings:DeliveryDbContext", _postgresContainer!.GetConnectionString());
        builder.UseSetting("ConnectionStrings:rabbitmq", _rabbitMqContainer!.GetConnectionString());
        builder.UseSetting("OrderService:BaseUrl", "http://localhost:5001");
        builder.UseSetting("IAM:RegistrationDelaySeconds", "0");

        // Disable HTTPS redirection in test environment to avoid redirect loops
        builder.UseSetting("ASPNETCORE_URLS", "http://+:0");

        builder.ConfigureTestServices(services => {
            // Bypass authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

            // Register fake services
            services.AddSingleton<Api.Services.IFileStorageService, Fakes.FakeFileStorageService>();

            // Replace real MassTransit (with RabbitMQ) with in-memory test harness
            services.AddMassTransitTestHarness();

            // Pre-mark IAM registration as done so health checks pass
            var statusTracker = new IAMRegistrationStatusTracker();
            statusTracker.MarkRegistered();
            services.AddSingleton(statusTracker);

            // Disable background IAM registration service
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(IHostedService) &&
                d.ImplementationType?.Name == "BackgroundIAMRegistrationService").ToList();
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }
        });
    }
}

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<DeliveryServiceTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
