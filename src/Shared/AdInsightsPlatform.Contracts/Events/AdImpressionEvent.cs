using AdInsightsPlatform.Contracts.Constants;

namespace AdInsightsPlatform.Contracts.Events;

/// <summary>
/// Raised when an advertisement is displayed (rendered) to a user.
/// Published to Kafka topic: raw-events (partitioned by TenantId).
/// </summary>
public sealed record AdImpressionEvent : BaseEvent
{
    public override string EventType => EventTypes.AdImpression;

    public required string CampaignId { get; init; }

    public required string AdId { get; init; }

    public required string UserId { get; init; }

    public bool IsViewable { get; init; }

    public int? ViewDurationMs { get; init; }

    public string? PageUrl { get; init; }
}
