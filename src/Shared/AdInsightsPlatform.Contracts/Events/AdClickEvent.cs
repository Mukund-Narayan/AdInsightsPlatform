using AdInsightsPlatform.Contracts.Constants;

namespace AdInsightsPlatform.Contracts.Events;

/// <summary>
/// Raised when a user clicks on an advertisement.
/// Published to Kafka topic: raw-events (partitioned by TenantId).
/// </summary>
public sealed record AdClickEvent : BaseEvent
{
    public override string EventType => EventTypes.AdClick;

    public required string CampaignId { get; init; }

    public required string AdId { get; init; }

    public required string UserId { get; init; }

    public string? ProductId { get; init; }

    public string? PageUrl { get; init; }

    public string? UserAgent { get; init; }

    public string? IpAddress { get; init; }
}
