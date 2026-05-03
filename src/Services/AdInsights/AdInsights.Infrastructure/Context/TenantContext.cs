using AdInsights.Application.Common.Interfaces;

namespace AdInsights.Infrastructure.Context;

/// <summary>
/// Scoped implementation of <see cref="ITenantContext"/>.
/// Populated per-request by <c>TenantResolutionMiddleware</c> after JWT validation.
/// Registered as <c>Scoped</c> so each HTTP request gets its own instance.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    private string? _tenantId;

    /// <inheritdoc />
    public string TenantId => _tenantId
        ?? throw new InvalidOperationException(
            "TenantContext has not been resolved. Ensure TenantResolutionMiddleware runs before this point.");

    /// <inheritdoc />
    public bool IsResolved => _tenantId is not null;

    /// <summary>
    /// Sets the tenant identifier. Called exclusively by <c>TenantResolutionMiddleware</c>.
    /// </summary>
    public void SetTenantId(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        _tenantId = tenantId;
    }
}
