using DotNet.Testcontainers.Containers;
using Maliev.DeliveryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Maliev.DeliveryService.Tests.Testing;

public class PostgreSqlTestFixture : IAsyncLifetime
{
    private static PostgreSqlContainer? _postgresContainer;
    private static readonly SemaphoreSlim _initLock = new(1, 1);
    private static bool _schemaInitialized;
    private static Exception? _initException;

    public string ConnectionString => _postgresContainer!.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (_postgresContainer?.State == TestcontainersStates.Running)
            {
                if (_schemaInitialized)
                    return;

                if (_initException != null)
                    throw _initException;

                try
                {
                    await CreateSchemaAsync();
                    _schemaInitialized = true;
                }
                catch (Exception ex)
                {
                    _initException = ex;
                    throw;
                }

                return;
            }

            _postgresContainer = 
                #pragma warning disable CS0618
        new PostgreSqlBuilder().WithImage("postgres:18-alpine")
                .Build();
#pragma warning restore CS0618

            await _postgresContainer.StartAsync();

            var ready = false;
            var retryCount = 0;
            while (!ready && retryCount < 60)
            {
                try
                {
                    await using var conn = new NpgsqlConnection(_postgresContainer.GetConnectionString());
                    await conn.OpenAsync();
                    ready = true;
                }
                catch
                {
                    retryCount++;
                    await Task.Delay(1000);
                }
            }

            if (!ready)
            {
                throw new InvalidOperationException("PostgreSQL container failed to start");
            }

            try
            {
                await CreateSchemaAsync();
                _schemaInitialized = true;
            }
            catch (Exception ex)
            {
                _initException = ex;
                throw;
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task CreateSchemaAsync()
    {
        var options = new DbContextOptionsBuilder<DeliveryDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var context = new DeliveryDbContext(options);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    public DeliveryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DeliveryDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new DeliveryDbContext(options);
    }
}

[CollectionDefinition("PostgreSqlDatabase")]
public class PostgreSqlTestCollection : ICollectionFixture<PostgreSqlTestFixture>
{
}



