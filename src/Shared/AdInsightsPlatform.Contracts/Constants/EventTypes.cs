namespace AdInsightsPlatform.Contracts.Constants;

/// <summary>
/// String constants for event type discriminators.
/// Used as Kafka message headers and for Flink stream routing.
/// </summary>
public static class EventTypes
{
    public const string AdClick = "AdClick";
    public const string AdImpression = "AdImpression";
    public const string AddToCart = "AddToCart";
    public const string ProductView = "ProductView";
    public const string Purchase = "Purchase";
}
