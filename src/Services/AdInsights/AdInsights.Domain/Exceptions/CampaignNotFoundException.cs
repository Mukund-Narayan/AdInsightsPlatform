namespace AdInsights.Domain.Exceptions;

/// <summary>
/// Thrown when a campaign cannot be found for the given campaignId + tenantId combination.
/// Maps to HTTP 404 Not Found in the global exception handler.
/// </summary>
public sealed class CampaignNotFoundException : DomainException
{
    private const string DefaultErrorCode = "CAMPAIGN_NOT_FOUND";

    public CampaignNotFoundException(string campaignId, string tenantId)
        : base(
            $"Campaign '{campaignId}' was not found for tenant '{tenantId}'.",
            DefaultErrorCode)
    {
    }
}
