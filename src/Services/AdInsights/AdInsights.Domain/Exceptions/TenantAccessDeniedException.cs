namespace AdInsights.Domain.Exceptions;

/// <summary>
/// Thrown when a caller attempts to access data belonging to a different tenant.
/// Maps to HTTP 403 Forbidden in the global exception handler.
/// </summary>
public sealed class TenantAccessDeniedException : DomainException
{
    private const string DefaultErrorCode = "TENANT_ACCESS_DENIED";

    public TenantAccessDeniedException(string requestedTenantId, string callerTenantId)
        : base(
            $"Caller from tenant '{callerTenantId}' is not authorised to access resources of tenant '{requestedTenantId}'.",
            DefaultErrorCode)
    {
    }
}
