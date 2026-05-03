namespace AdInsights.Domain.Enums;

/// <summary>
/// Lifecycle status of an advertising campaign.
/// </summary>
public enum CampaignStatus
{
    Draft = 0,
    Active = 1,
    Paused = 2,
    Completed = 3,
    Archived = 4
}
