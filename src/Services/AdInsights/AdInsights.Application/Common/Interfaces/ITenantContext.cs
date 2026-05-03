namespace AdInsights.Application.Common.Interfaces;

/// <summary>
/// Provides the current request's tenant identity to the application layer.
/// Populated by <c>TenantResolutionMiddleware</c> from the JWT or request header,
/// then injected into query handlers via constructor DI.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The tenant identifier extracted from the current HTTP request.
    /// Guaranteed non-null when a request reaches a protected endpoint.
    /// </summary>
    string TenantId { get; }

    /// <summary>
    /// Returns true when a tenant context has been successfully resolved for the request.
    /// </summary>
    bool IsResolved { get; }
}
