using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Erp.Api.Caching;

public sealed class CacheService(IDistributedCache cache, ILogger<CacheService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cached = await cache.GetStringAsync(key, cancellationToken);
            if (!string.IsNullOrWhiteSpace(cached))
            {
                var value = JsonSerializer.Deserialize<T>(cached, JsonOptions);
                if (value is not null)
                {
                    return value;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache read failed for {CacheKey}. Falling back to source data.", key);
        }

        var created = await factory();

        try
        {
            await cache.SetStringAsync(
                key,
                JsonSerializer.Serialize(created, JsonOptions),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache write failed for {CacheKey}. Returning source data without caching.", key);
        }

        return created;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache remove failed for {CacheKey}.", key);
        }
    }
}

public static class CacheKeys
{
    public static string SideNav(int roleId) => $"erp:side-nav:role:{roleId}";
    public static string Roles => "erp:roles:all";
    public static string Users => "erp:users:all";
    public static string Departments => "erp:departments:all";
}
