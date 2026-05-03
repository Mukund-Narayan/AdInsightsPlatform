namespace EventCollector.Application.DTOs;

/// <summary>
/// Inbound DTO for the POST /api/v1/events endpoint.
/// Accepts events from retailer SDKs, websites, and mobile apps.
/// </summary>
public sealed record EventIngestionRequest
{
    /// <summary>
    /// The type of event. Must match one of the known event type strings:
    /// "AdClick", "AdImpression", "AddToCart", "ProductView", "Purchase".
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// The tenant sending this event. When sent via the API Gateway with JWT,
    /// this is populated from the JWT claim and overrides any value in the body.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>The campaign this event relates to.</summary>
    public required string CampaignId { get; init; }

    /// <summary>The specific ad unit involved.</summary>
    public string? AdId { get; init; }

    /// <summary>Anonymised user identifier.</summary>
    public required string UserId { get; init; }

    /// <summary>The product involved (for AddToCart and ProductView events).</summary>
    public string? ProductId { get; init; }

    /// <summary>Client-side timestamp of the event (UTC). Defaults to server time if null.</summary>
    public DateTimeOffset? ClientTimestamp { get; init; }

    /// <summary>Additional key-value metadata for extensibility.</summary>
    public Dictionary<string, string>? Metadata { get; init; }
}
