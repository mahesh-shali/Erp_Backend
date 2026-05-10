using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Erp.Api.Caching;

public sealed class CacheService(IDistributedCache cache)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
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

        var created = await factory();
        await cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(created, JsonOptions),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            },
            cancellationToken);

        return created;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return cache.RemoveAsync(key, cancellationToken);
    }
}

public static class CacheKeys
{
    public static string SideNav(int roleId) => $"erp:side-nav:role:{roleId}";
    public static string Roles => "erp:roles:all";
    public static string Users => "erp:users:all";
    public static string Departments => "erp:departments:all";
}
