using AdInsights.Application.DTOs;
using AdInsights.Application.Queries.GetAdClicks;
using AdInsights.Domain.Repositories;
using AdInsights.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AdInsights.UnitTests.Queries;

/// <summary>
/// Unit tests for GetAdClicksQueryHandler.
/// Tests are independent — each test creates its own mocks (CQ-039).
/// Naming convention: MethodName_Condition_ExpectedOutcome (CQ-040).
/// </summary>
public sealed class GetAdClicksQueryHandlerTests
{
    private readonly Mock<IAdMetricsRepository> _repositoryMock;
    private readonly GetAdClicksQueryHandler _handler;

    public GetAdClicksQueryHandlerTests()
    {
        _repositoryMock = new Mock<IAdMetricsRepository>(MockBehavior.Strict);
        _handler = new GetAdClicksQueryHandler(
            _repositoryMock.Object,
            NullLogger<GetAdClicksQueryHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsCount_ReturnsCorrectResponse()
    {
        // Arrange
        const long expectedClicks = 1500L;
        var query = CreateQuery();

        _repositoryMock
            .Setup(r => r.GetClicksAsync(
                query.CampaignId,
                query.TenantId,
                It.IsAny<TimePeriod>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedClicks);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(expectedClicks);
        result.CampaignId.Should().Be(query.CampaignId);
        result.MetricType.Should().Be("Clicks");
        result.From.Should().Be(query.From);
        result.To.Should().Be(query.To);
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsZero_ReturnsZeroCount()
    {
        // Arrange
        var query = CreateQuery();

        _repositoryMock
            .Setup(r => r.GetClicksAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimePeriod>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Count.Should().Be(0L);
    }

    [Fact]
    public async Task Handle_WhenDateRangeWithinLast30Days_SetsIsRealTimeTrue()
    {
        // Arrange
        var query = CreateQuery(
            from: DateTimeOffset.UtcNow.AddDays(-7),
            to: DateTimeOffset.UtcNow);

        _repositoryMock
            .Setup(r => r.GetClicksAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimePeriod>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(100L);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsRealTime.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenDateRangeOlderThan30Days_SetsIsRealTimeFalse()
    {
        // Arrange
        var query = CreateQuery(
            from: DateTimeOffset.UtcNow.AddDays(-90),
            to: DateTimeOffset.UtcNow.AddDays(-60));

        _repositoryMock
            .Setup(r => r.GetClicksAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimePeriod>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(200L);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsRealTime.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        var query = CreateQuery();

        _repositoryMock
            .Setup(r => r.GetClicksAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimePeriod>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection failure"));

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert — handler should not swallow exceptions (CQ-020)
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Connection failure");
    }

    [Fact]
    public async Task Handle_CallsRepositoryWithCorrectParameters()
    {
        // Arrange
        var query = CreateQuery();

        _repositoryMock
            .Setup(r => r.GetClicksAsync(
                query.CampaignId,
                query.TenantId,
                It.Is<TimePeriod>(p => p.From == query.From && p.To == query.To),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(42L);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert — verify exact parameters passed to repository
        _repositoryMock.VerifyAll();
    }

    private static GetAdClicksQuery CreateQuery(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null)
    {
        return new GetAdClicksQuery
        {
            CampaignId = "campaign-abc-123",
            TenantId = "tenant-walmart-001",
            From = from ?? DateTimeOffset.UtcNow.AddDays(-1),
            To = to ?? DateTimeOffset.UtcNow
        };
    }
}
