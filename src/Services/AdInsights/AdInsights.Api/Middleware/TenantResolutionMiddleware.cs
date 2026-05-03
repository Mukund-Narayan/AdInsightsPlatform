using AdInsights.Infrastructure.Context;

namespace AdInsights.Api.Middleware;

/// <summary>
/// Resolves the tenant identifier from the incoming HTTP request and populates
/// the scoped <see cref="TenantContext"/> for use throughout the request pipeline.
///
/// Resolution order:
///   1. JWT claim "tenant_id" (preferred — cryptographically verified)
///   2. HTTP header "X-Tenant-Id" (for service-to-service calls with pre-validated tokens)
///
/// If neither is present on a protected endpoint, the request continues and the endpoint's
/// [Authorize] policy will return 401. The tenant context will throw if accessed without resolution.
/// </summary>
public sealed class TenantResolutionMiddleware : IMiddleware
{
    private const string TenantIdClaimType = "tenant_id";
    private const string TenantIdHeaderName = "X-Tenant-Id";

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var tenantContext = context.RequestServices.GetRequiredService<TenantContext>();

        var tenantId = ResolveTenantId(context);

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            tenantContext.SetTenantId(tenantId);
        }

        await next(context);
    }

    private static string? ResolveTenantId(HttpContext context)
    {
        // Prefer JWT claim — it's cryptographically signed and verified by JwtBearer middleware
        var jwtClaim = context.User.FindFirst(TenantIdClaimType)?.Value;

        if (!string.IsNullOrWhiteSpace(jwtClaim))
        {
            return jwtClaim;
        }

        // Fallback: service-to-service header (only valid when behind the API Gateway)
        return context.Request.Headers.TryGetValue(TenantIdHeaderName, out var headerValue)
            ? headerValue.ToString()
            : null;
    }
}
