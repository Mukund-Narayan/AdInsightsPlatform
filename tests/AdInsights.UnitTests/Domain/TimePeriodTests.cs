using AdInsights.Domain.ValueObjects;
using FluentAssertions;

namespace AdInsights.UnitTests.Domain;

/// <summary>
/// Unit tests for the TimePeriod value object.
/// Validates the hot-path/cold-path routing logic that underpins HybridAdMetricsRepository.
/// </summary>
public sealed class TimePeriodTests
{
    [Fact]
    public void Constructor_WhenFromAfterTo_ThrowsArgumentException()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow;
        var to = from.AddSeconds(-1);

        // Act
        var act = () => new TimePeriod(from, to);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*from*to*");
    }

    [Fact]
    public void Constructor_WhenFromEqualsTo_DoesNotThrow()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;

        // Act
        var act = () => new TimePeriod(now, now);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void IsRealTime_WhenPeriodWithinLast30Days_ReturnsTrue()
    {
        // Arrange
        var period = new TimePeriod(
            DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow);

        // Act & Assert
        period.IsRealTime().Should().BeTrue();
    }

    [Fact]
    public void IsRealTime_WhenPeriodStartedOver30DaysAgo_ReturnsFalse()
    {
        // Arrange
        var period = new TimePeriod(
            DateTimeOffset.UtcNow.AddDays(-60),
            DateTimeOffset.UtcNow.AddDays(-45));

        // Act & Assert
        period.IsRealTime().Should().BeFalse();
    }

    [Fact]
    public void IsHistorical_WhenPeriodOlderThan30Days_ReturnsTrue()
    {
        // Arrange
        var period = new TimePeriod(
            DateTimeOffset.UtcNow.AddDays(-90),
            DateTimeOffset.UtcNow.AddDays(-60));

        // Act & Assert
        period.IsHistorical().Should().BeTrue();
    }

    [Fact]
    public void SpansBothPaths_WhenPeriodCrossesThe30DayBoundary_ReturnsTrue()
    {
        // Arrange — starts before 30-day cutoff, ends after it (within last 30 days)
        var period = new TimePeriod(
            DateTimeOffset.UtcNow.AddDays(-40),
            DateTimeOffset.UtcNow.AddDays(-1));

        // Act & Assert
        period.SpansBothPaths().Should().BeTrue();
    }

    [Fact]
    public void DurationInDays_ReturnsCorrectNumberOfDays()
    {
        // Arrange
        var period = new TimePeriod(
            DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow);

        // Act & Assert
        period.DurationInDays().Should().Be(7);
    }

    [Fact]
    public void LastDays_ReturnsCorrectPeriod()
    {
        // Act — use 29 days to avoid boundary race between LastDays() clock and IsRealTime() clock
        var period = TimePeriod.LastDays(29);

        // Assert
        period.IsRealTime().Should().BeTrue();
        period.DurationInDays().Should().Be(29);
    }
}
