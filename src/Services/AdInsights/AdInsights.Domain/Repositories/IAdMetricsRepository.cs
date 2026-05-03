using AdInsights.Domain.ValueObjects;

namespace AdInsights.Domain.Repositories;

/// <summary>
/// Defines the data access contract for ad metrics queries.
/// The concrete implementation (Cassandra / ClickHouse / Hybrid) is resolved
/// by the DI container — the domain layer has zero knowledge of infrastructure.
/// </summary>
public interface IAdMetricsRepository
{
    /// <summary>
    /// Returns the total number of ad clicks for a campaign within the specified period.
    /// </summary>
    /// <param name="campaignId">The campaign identifier.</param>
    /// <param name="tenantId">The tenant identifier (data isolation boundary).</param>
    /// <param name="period">The time window to aggregate over.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    Task<long> GetClicksAsync(
        string campaignId,
        string tenantId,
        TimePeriod period,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the total number of ad impressions for a campaign within the specified period.
    /// </summary>
    Task<long> GetImpressionsAsync(
        string campaignId,
        string tenantId,
        TimePeriod period,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the number of click-to-basket conversions for a campaign within the specified period.
    /// A conversion is counted when a user adds a product to cart within 30 minutes of clicking an ad.
    /// </summary>
    Task<long> GetClickToBasketAsync(
        string campaignId,
        string tenantId,
        TimePeriod period,
        CancellationToken cancellationToken);
}
