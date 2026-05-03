using System.Text.Json;
using AdInsights.Domain.Repositories;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AdInsights.Infrastructure.Caching;

/// <summary>
/// Redis-backed implementation of <see cref="ICacheRepository"/>.
/// Uses <see cref="IConnectionMultiplexer"/> for connection pooling and resilience.
/// JSON serialisation via System.Text.Json (built-in, zero extra dependencies).
///
/// Thread-safety: IDatabase is stateless and thread-safe per StackExchange.Redis design.
/// </summary>
public sealed class RedisCacheRepository : ICacheRepository
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheRepository> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RedisCacheRepository(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisCacheRepository> logger)
    {
        _database = connectionMultiplexer.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var value = await _database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(value!, JsonOptions);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for key={Key}. Returning cache miss.", key);
            return null;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan timeToLive,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var serialised = JsonSerializer.Serialize(value, JsonOptions);
            await _database.StringSetAsync(key, serialised, timeToLive);
        }
        catch (RedisException ex)
        {
            // Cache write failures should not fail the request — log as warning
            _logger.LogWarning(ex, "Redis SET failed for key={Key}. Cache write skipped.", key);
        }
    }

    public async Task InvalidateAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis DELETE failed for key={Key}.", key);
        }
    }
}
