using AdInsightsPlatform.Contracts.Constants;

namespace AdInsightsPlatform.Contracts.Events;

/// <summary>
/// Raised when a user adds a product to their shopping cart.
/// Used by Flink CEP to correlate with prior AdClick events
/// within a 30-minute session window to produce ClickToBasket metrics.
/// </summary>
public sealed record AddToCartEvent : BaseEvent
{
    public override string EventType => EventTypes.AddToCart;

    public required string CampaignId { get; init; }

    public required string ProductId { get; init; }

    public required string UserId { get; init; }

    public int Quantity { get; init; } = 1;

    public decimal? UnitPrice { get; init; }

    public string? CurrencyCode { get; init; }
}
