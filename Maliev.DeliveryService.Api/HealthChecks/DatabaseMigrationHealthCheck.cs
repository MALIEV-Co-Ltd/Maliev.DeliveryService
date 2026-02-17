using Maliev.DeliveryService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Maliev.DeliveryService.Api.HealthChecks;

public class DatabaseMigrationHealthCheck : IHealthCheck
{
    private readonly DeliveryDbContext _context;
    private readonly ILogger<DatabaseMigrationHealthCheck> _logger;

    public DatabaseMigrationHealthCheck(
        DeliveryDbContext context,
        ILogger<DatabaseMigrationHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if all migrations have been applied
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
            var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync(cancellationToken);

            if (pendingMigrations.Any())
            {
                var pendingList = string.Join(", ", pendingMigrations);
                _logger.LogWarning("Database has pending migrations: {PendingMigrations}", pendingList);

                return HealthCheckResult.Degraded(
                    $"Database has {pendingMigrations.Count()} pending migrations: {pendingList}",
                    data: new Dictionary<string, object>
                    {
                        { "pending_migrations", pendingMigrations.ToList() },
                        { "applied_migrations_count", appliedMigrations.Count() }
                    });
            }

            _logger.LogDebug("All database migrations applied. Total: {Count}", appliedMigrations.Count());

            return HealthCheckResult.Healthy(
                $"All migrations applied ({appliedMigrations.Count()} total)",
                data: new Dictionary<string, object>
                {
                    { "applied_migrations_count", appliedMigrations.Count() },
                    { "latest_migration", appliedMigrations.LastOrDefault() ?? "none" }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check database migration status");
            return HealthCheckResult.Unhealthy("Failed to check database migrations", ex);
        }
    }
}
