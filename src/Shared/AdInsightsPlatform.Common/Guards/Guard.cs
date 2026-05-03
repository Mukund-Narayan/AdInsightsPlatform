namespace AdInsightsPlatform.Common.Guards;

/// <summary>
/// Provides argument validation utilities following a fail-fast strategy.
/// Eliminates repeated null/empty checks across the codebase (CQ-011).
/// </summary>
public static class Guard
{
    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> when <paramref name="value"/> is null.
    /// </summary>
    /// <typeparam name="T">Reference or nullable type being checked.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">Name of the parameter (populated automatically by caller).</param>
    /// <returns>The non-null value.</returns>
    public static T NotNull<T>(
        T? value,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> when <paramref name="value"/> is null, empty, or whitespace.
    /// </summary>
    public static string NotNullOrWhiteSpace(
        string? value,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be null or whitespace.", paramName);
        }

        return value;
    }

    /// <summary>
    /// Throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="value"/> is less than <paramref name="minimum"/>.
    /// </summary>
    public static T NotLessThan<T>(
        T value,
        T minimum,
        [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(minimum) < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"Value must be >= {minimum}.");
        }

        return value;
    }
}
