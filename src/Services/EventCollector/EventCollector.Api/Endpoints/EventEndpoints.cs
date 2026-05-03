using EventCollector.Application.Commands.PublishEvent;
using EventCollector.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;


namespace EventCollector.Api.Endpoints;

/// <summary>
/// Maps all event ingestion HTTP endpoints.
/// This is the primary entry point for retailer SDKs and JavaScript tags
/// to push customer interaction events to the platform.
/// </summary>
public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/events")
            .WithTags("Event Ingestion")            
            .WithOpenApi();

        group.MapPost("/", IngestEventAsync)
            .WithName("IngestEvent")
            .WithSummary("Ingest a single ad interaction event")
            .WithDescription(
                "Accepts a single event (AdClick, AdImpression, AddToCart, etc.) and " +
                "publishes it durably to Kafka for stream processing by Flink.")
            .Produces<EventIngestionResponse>(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/batch", IngestBatchAsync)
            .WithName("IngestBatchEvents")
            .WithSummary("Ingest a batch of events (max 100 per request)")
            .Produces<IEnumerable<EventIngestionResponse>>(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> IngestEventAsync(
        [FromBody] EventIngestionRequest request,
        [FromHeader(Name = "X-Tenant-Id")] string? headerTenantId,
        HttpContext context,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenanId(context, headerTenantId);

        var command = new PublishEventCommand
        {
            Request = request,
            TenantId = tenantId
        };

        var result = await mediator.Send(command, cancellationToken);
        return Results.Accepted(null, result);
    }

    private static async Task<IResult> IngestBatchAsync(
        [FromBody] IReadOnlyList<EventIngestionRequest> requests,
        [FromHeader(Name = "X-Tenant-Id")] string? headerTenantId,
        HttpContext context,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        if (requests.Count > 100)
        {
            return Results.BadRequest("Batch size must not exceed 100 events.");
        }

        var tenantId = ResolveTenanId(context, headerTenantId);

        var tasks = requests.Select(request => mediator.Send(new PublishEventCommand
        {
            Request = request,
            TenantId = tenantId
        }, cancellationToken));

        var results = await Task.WhenAll(tasks);
        return Results.Accepted(null, results);
    }

    private static string ResolveTenanId(HttpContext context, string? headerTenantId)
    {
        return context.User.FindFirst("tenant_id")?.Value
            ?? headerTenantId
            ?? throw new InvalidOperationException("TenantId could not be resolved from JWT or header.");
    }
}
