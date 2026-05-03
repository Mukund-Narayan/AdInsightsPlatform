using AdInsights.Application.Common.Interfaces;
using AdInsights.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AdInsights.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that implements the Cache-Aside pattern.
/// Applied automatically to any query that implements <see cref="ICacheableQuery"/>.
///
/// Flow:
///   1. Check Redis for cached result → return immediately on HIT (sub-ms response)
///   2. On cache MISS → execute the next handler in the pipeline
///   3. Store the result in Redis with the query's configured TTL
///   4. Return the fresh result
///
/// This behavior is registered before <c>LoggingBehavior</c> and <c>ValidationBehavior</c>
/// in the DI pipeline so that cached responses bypass logging overhead on hot paths.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly ICacheRepository _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(
        ICacheRepository cache,
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery cacheableQuery)
        {
            return await next();
        }

        var cachedValue = await _cache.GetAsync<TResponse>(cacheableQuery.CacheKey, cancellationToken);

        if (cachedValue is not null)
        {
            _logger.LogDebug("Cache HIT for key={CacheKey}", cacheableQuery.CacheKey);
            return cachedValue;
        }

        _logger.LogDebug("Cache MISS for key={CacheKey}", cacheableQuery.CacheKey);

        var response = await next();

        await _cache.SetAsync(
            cacheableQuery.CacheKey,
            response,
            cacheableQuery.CacheDuration,
            cancellationToken);

        return response;
    }
}
