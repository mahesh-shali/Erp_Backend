using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Data;

public static class DatabaseMigrator
{
    public static async Task<MigrationCheckResult> MigrateLatestAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var migrations = db.Database.GetMigrations().ToList();
        var latestMigration = migrations.LastOrDefault();
        if (latestMigration is null)
        {
            logger.LogInformation("No EF Core migrations exist for this project.");
            return new MigrationCheckResult(null, null, MigrationApplied: false);
        }

        var appliedMigrations = (await db.Database.GetAppliedMigrationsAsync()).ToList();
        var latestAppliedMigration = appliedMigrations.LastOrDefault();

        if (latestAppliedMigration == latestMigration)
        {
            logger.LogInformation("Database is already on latest migration {MigrationId}.", latestMigration);
            return new MigrationCheckResult(latestMigration, latestAppliedMigration, MigrationApplied: false);
        }

        logger.LogInformation(
            "Database migration required. Latest applied: {LatestAppliedMigration}; target: {LatestMigration}.",
            latestAppliedMigration ?? "none",
            latestMigration);

        await db.Database.MigrateAsync();
        return new MigrationCheckResult(latestMigration, latestAppliedMigration, MigrationApplied: true);
    }
}

public sealed record MigrationCheckResult(string? LatestMigration, string? LatestAppliedMigration, bool MigrationApplied);
