using AdInsights.Application.Common.Interfaces;
using AdInsights.Application.DTOs;
using AdInsights.Application.Queries.GetAdClicks;
using AdInsights.Application.Queries.GetAdImpressions;
using AdInsights.Application.Queries.GetClickToBasket;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AdInsights.Api.Endpoints;

/// <summary>
/// Maps all ad metrics HTTP endpoints using ASP.NET Core Minimal API conventions.
/// Endpoints follow RESTful resource naming and return consistent AdMetricsResponse DTOs.
///
/// All endpoints:
///   - Require JWT authentication (configured in Program.cs)
///   - Validate the caller's tenantId matches the resource's tenantId
///   - Support both real-time (≤30 days, served from Cassandra)
///     and historical (>30 days, served from ClickHouse) queries
///   - Return RFC 7807 Problem Details on errors
/// </summary>
public static class AdMetricsEndpoints
{
    /// <summary>
    /// Registers all ad metrics endpoints under /api/v1/ad.
    /// </summary>
    public static IEndpointRouteBuilder MapAdMetricsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ad")
            .WithTags("Ad Metrics")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGet("/{campaignId}/clicks", GetClicksAsync)
            .WithName("GetAdClicks")
            .WithSummary("Get total ad clicks for a campaign")
            .WithDescription(
                "Returns the total number of unique click events for the specified campaign " +
                "within the given date range. Queries Cassandra for recent data (≤30 days) " +
                "and ClickHouse for historical data (>30 days).")
            .Produces<AdMetricsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{campaignId}/impressions", GetImpressionsAsync)
            .WithName("GetAdImpressions")
            .WithSummary("Get total ad impressions for a campaign")
            .WithDescription(
                "Returns the total number of times the ad was displayed to users " +
                "within the given date range.")
            .Produces<AdMetricsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{campaignId}/clickToBasket", GetClickToBasketAsync)
            .WithName("GetClickToBasket")
            .WithSummary("Get click-to-basket conversions for a campaign")
            .WithDescription(
                "Returns the number of users who added a product to cart within 30 minutes " +
                "of clicking on this campaign's ad (last-click attribution model). " +
                "Computed by Flink CEP.")
            .Produces<AdMetricsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> GetClicksAsync(
        string campaignId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        ITenantContext tenantContext,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        var (resolvedFrom, resolvedTo) = ResolveDateRange(from, to);

        var query = new GetAdClicksQuery
        {
            CampaignId = campaignId,
            TenantId = tenantContext.TenantId,
            From = resolvedFrom,
            To = resolvedTo
        };

        var result = await mediator.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetImpressionsAsync(
        string campaignId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        ITenantContext tenantContext,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        var (resolvedFrom, resolvedTo) = ResolveDateRange(from, to);

        var query = new GetAdImpressionsQuery
        {
            CampaignId = campaignId,
            TenantId = tenantContext.TenantId,
            From = resolvedFrom,
            To = resolvedTo
        };

        var result = await mediator.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetClickToBasketAsync(
        string campaignId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        ITenantContext tenantContext,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        var (resolvedFrom, resolvedTo) = ResolveDateRange(from, to);

        var query = new GetClickToBasketQuery
        {
            CampaignId = campaignId,
            TenantId = tenantContext.TenantId,
            From = resolvedFrom,
            To = resolvedTo
        };

        var result = await mediator.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    /// <summary>
    /// Resolves the date range from query parameters.
    /// Defaults to the last 24 hours if not specified.
    /// </summary>
    private static (DateTimeOffset From, DateTimeOffset To) ResolveDateRange(
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        var resolvedTo = to ?? DateTimeOffset.UtcNow;
        var resolvedFrom = from ?? resolvedTo.AddHours(-24);
        return (resolvedFrom, resolvedTo);
    }
}
