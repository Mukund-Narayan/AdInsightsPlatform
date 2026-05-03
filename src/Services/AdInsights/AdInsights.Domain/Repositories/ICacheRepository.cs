namespace AdInsights.Domain.Repositories;

/// <summary>
/// Defines the caching contract used by the application layer via the CachingBehavior pipeline.
/// The concrete implementation (Redis) is resolved by the DI container.
/// </summary>
public interface ICacheRepository
{
    /// <summary>
    /// Retrieves a cached value by key. Returns null on cache miss.
    /// </summary>
    /// <typeparam name="T">The type to deserialise the cached value into.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        where T : class;

    /// <summary>
    /// Stores a value in the cache with the specified TTL.
    /// </summary>
    /// <typeparam name="T">The type to serialise.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="timeToLive">How long the entry should remain valid.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    Task SetAsync<T>(string key, T value, TimeSpan timeToLive, CancellationToken cancellationToken)
        where T : class;

    /// <summary>
    /// Removes a cached entry. Used for explicit cache invalidation.
    /// </summary>
    Task InvalidateAsync(string key, CancellationToken cancellationToken);
}
