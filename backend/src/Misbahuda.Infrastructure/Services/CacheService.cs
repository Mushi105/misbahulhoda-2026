using System.Text.Json;
using Misbahuda.Application.Interfaces;
using StackExchange.Redis;

namespace Misbahuda.Infrastructure.Services;

public class CacheService(IConnectionMultiplexer redis) : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return default;
        return JsonSerializer.Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var serialized = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, serialized, expiry ?? TimeSpan.FromMinutes(30));
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        await _db.KeyDeleteAsync(key);

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
        await _db.KeyExistsAsync(key);
}
