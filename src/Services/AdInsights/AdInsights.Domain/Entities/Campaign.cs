using AdInsights.Domain.Enums;

namespace AdInsights.Domain.Entities;

/// <summary>
/// Represents an advertising campaign owned by a specific tenant.
/// Campaigns are the primary grouping unit for ad metrics.
/// </summary>
public sealed class Campaign
{
    public string Id { get; private set; }

    public string TenantId { get; private set; }

    public string Name { get; private set; }

    public CampaignStatus Status { get; private set; }

    public DateTimeOffset StartDate { get; private set; }

    public DateTimeOffset EndDate { get; private set; }

    // Private constructor for ORM hydration — properties are always set via Create()
    // null! suppresses CS8618 — these are initialised by the factory method before use
    private Campaign()
    {
        Id = null!;
        TenantId = null!;
        Name = null!;
    }

    /// <summary>
    /// Creates a new campaign. Use this factory method to enforce invariants.
    /// </summary>
    public static Campaign Create(
        string id,
        string tenantId,
        string name,
        DateTimeOffset startDate,
        DateTimeOffset endDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (endDate <= startDate)
        {
            throw new ArgumentException("End date must be after start date.");
        }

        return new Campaign
        {
            Id = id,
            TenantId = tenantId,
            Name = name,
            Status = CampaignStatus.Draft,
            StartDate = startDate,
            EndDate = endDate
        };
    }

    /// <summary>
    /// Returns true if the campaign is currently active and delivering ads.
    /// </summary>
    public bool IsActive()
    {
        var now = DateTimeOffset.UtcNow;
        return Status == CampaignStatus.Active
               && StartDate <= now
               && EndDate >= now;
    }

    /// <summary>
    /// Returns true if this campaign belongs to the specified tenant.
    /// Used for authorisation checks in the application layer.
    /// </summary>
    public bool BelongsToTenant(string tenantId)
    {
        return string.Equals(TenantId, tenantId, StringComparison.OrdinalIgnoreCase);
    }
}
