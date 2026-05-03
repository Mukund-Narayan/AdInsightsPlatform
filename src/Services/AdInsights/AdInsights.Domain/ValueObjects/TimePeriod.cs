namespace AdInsights.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing a date/time range for metric queries.
/// Encapsulates the hot-path vs cold-path routing logic — keeping infrastructure
/// routing decisions out of the application layer (Clean Architecture boundary).
/// </summary>
public sealed record TimePeriod
{
    /// <summary>Maximum age in days for data served from Cassandra (hot path).</summary>
    private const int HotPathMaxDays = 30;

    /// <summary>Inclusive start of the time window (UTC).</summary>
    public DateTimeOffset From { get; }

    /// <summary>Inclusive end of the time window (UTC).</summary>
    public DateTimeOffset To { get; }

    /// <summary>
    /// Creates a validated <see cref="TimePeriod"/>.
    /// </summary>
    /// <param name="from">Start of the window (must be before <paramref name="to"/>).</param>
    /// <param name="to">End of the window (must not be in the future).</param>
    /// <exception cref="ArgumentException">When <paramref name="from"/> is after <paramref name="to"/>.</exception>
    public TimePeriod(DateTimeOffset from, DateTimeOffset to)
    {
        if (from > to)
        {
            throw new ArgumentException($"'from' ({from}) must be before or equal to 'to' ({to}).");
        }

        From = from;
        To = to;
    }

    /// <summary>
    /// Returns true when the entire period falls within the last 30 days —
    /// meaning Cassandra (hot path) can serve the query with sub-millisecond latency.
    /// </summary>
    public bool IsRealTime()
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-HotPathMaxDays);
        return From >= cutoff;
    }

    /// <summary>
    /// Returns true when the period is entirely older than 30 days —
    /// meaning ClickHouse (cold path) must be queried for historical data.
    /// </summary>
    public bool IsHistorical() => !IsRealTime() && To < DateTimeOffset.UtcNow.AddDays(-HotPathMaxDays);

    /// <summary>
    /// Returns true when the period spans both the hot and cold paths,
    /// requiring a merged result from both Cassandra and ClickHouse.
    /// </summary>
    public bool SpansBothPaths()
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-HotPathMaxDays);
        return From < cutoff && To >= cutoff;
    }

    /// <summary>Total duration of the period in whole days.</summary>
    public int DurationInDays() => (int)(To - From).TotalDays;

    /// <summary>Returns a <see cref="TimePeriod"/> representing the last N days up to now.</summary>
    public static TimePeriod LastDays(int days)
    {
        var now = DateTimeOffset.UtcNow;
        return new TimePeriod(now.AddDays(-days), now);
    }
}
