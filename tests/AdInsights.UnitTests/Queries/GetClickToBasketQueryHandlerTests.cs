using AdInsights.Application.Queries.GetClickToBasket;
using AdInsights.Domain.Repositories;
using AdInsights.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AdInsights.UnitTests.Queries;

/// <summary>
/// Unit tests for GetClickToBasketQueryHandler.
/// CTB metrics are the most complex — they require Flink CEP to have run first.
/// These tests verify the query handler correctly delegates to the repository.
/// </summary>
public sealed class GetClickToBasketQueryHandlerTests
{
    private readonly Mock<IAdMetricsRepository> _repositoryMock;
    private readonly GetClickToBasketQueryHandler _handler;

    public GetClickToBasketQueryHandlerTests()
    {
        _repositoryMock = new Mock<IAdMetricsRepository>(MockBehavior.Strict);
        _handler = new GetClickToBasketQueryHandler(
            _repositoryMock.Object,
            NullLogger<GetClickToBasketQueryHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenConversionsExist_ReturnsCorrectCount()
    {
        // Arrange
        const long expectedConversions = 42L;
        var query = CreateQuery();

        _repositoryMock
            .Setup(r => r.GetClickToBasketAsync(
                query.CampaignId,
                query.TenantId,
                It.IsAny<TimePeriod>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedConversions);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Count.Should().Be(expectedConversions);
        result.MetricType.Should().Be("ClickToBasket");
        result.CampaignId.Should().Be(query.CampaignId);
    }

    [Fact]
    public async Task Handle_WhenNoConversions_ReturnsZero()
    {
        // Arrange
        var query = CreateQuery();

        _repositoryMock
            .Setup(r => r.GetClickToBasketAsync(
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
    public async Task Handle_VerifiesRepositoryIsCalledOnce()
    {
        // Arrange
        var query = CreateQuery();

        _repositoryMock
            .Setup(r => r.GetClickToBasketAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimePeriod>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(10L);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert — ensure no double-calling
        _repositoryMock.Verify(
            r => r.GetClickToBasketAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimePeriod>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static GetClickToBasketQuery CreateQuery() => new()
    {
        CampaignId = "campaign-summer-sale",
        TenantId = "tenant-amazon-uk",
        From = DateTimeOffset.UtcNow.AddDays(-1),
        To = DateTimeOffset.UtcNow
    };
}
