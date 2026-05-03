namespace AdInsights.Application.Common.Interfaces;

/// <summary>
/// Marker interface applied to queries whose results should be cached
/// by the <c>CachingBehavior</c> MediatR pipeline behavior.
/// </summary>
public interface ICacheableQuery
{
    /// <summary>
    /// The Redis cache key for this query instance.
    /// Must uniquely identify the query parameters (campaignId, tenantId, date range).
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// How long the cached result should remain valid.
    /// Default: 30 seconds for real-time metrics, 5 minutes for historical.
    /// </summary>
    TimeSpan CacheDuration { get; }
}
