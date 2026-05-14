using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Erp.Api.Data;

public static class StartupStateStore
{
    public static async Task<string?> GetValueAsync(IServiceProvider services, string key)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await EnsureTableAsync(db);
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM erp_startup_state WHERE key = @key";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "key";
        parameter.Value = key;
        command.Parameters.Add(parameter);

        var value = await command.ExecuteScalarAsync();
        return value as string;
    }

    public static async Task SetValueAsync(IServiceProvider services, string key, string value)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await EnsureTableAsync(db);
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO erp_startup_state (key, value, updated_at)
            VALUES ({0}, {1}, now())
            ON CONFLICT (key)
            DO UPDATE SET value = EXCLUDED.value, updated_at = EXCLUDED.updated_at
            """,
            key,
            value);
    }

    private static Task EnsureTableAsync(AppDbContext db)
    {
        return db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS erp_startup_state (
                key text PRIMARY KEY,
                value text NOT NULL,
                updated_at timestamp with time zone NOT NULL
            )
            """);
    }
}
