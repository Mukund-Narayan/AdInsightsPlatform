namespace AdInsights.Domain.Exceptions;

/// <summary>
/// Base class for all domain-specific exceptions.
/// Inheriting types encode business rule violations and are mapped to
/// appropriate HTTP status codes by the global exception handler middleware.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>Machine-readable error code for API consumers.</summary>
    public string ErrorCode { get; }

    protected DomainException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    protected DomainException(string message, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
