using AwesomeAssertions;
using Events.Application.Abstractions.Caching;
using Events.Application.Abstractions.Repositories;
using Events.Application.DTOs;
using Events.Application.Queries;
using Events.Domain.Entities;
using Events.Tests.Infrastructure;
using Moq;
using Xunit;

namespace Events.Tests.Queries;

public class GetTopEventsHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly GetTopEventsQueryHandler _handler;

    public GetTopEventsHandlerTests()
    {
        _handler = new GetTopEventsQueryHandler(_eventRepositoryMock.Object, _cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenIdExistingInCache_ShouldReturnFromCacheServices()
    {
        // Arrange
        var existingEvents = Enumerable.Range(0, 10).Select(x => EventCacheDto.FromEntity(TestEventFactory.Create())).ToList();
        var key = CacheKeys.Top10Events;
        var query = new GetTopEventsQuery();

        _cacheServiceMock
            .Setup(cache => cache.GetAsync<IReadOnlyList<EventCacheDto>>(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvents);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(existingEvents.Count);
        result.Should().BeEquivalentTo(EventResponseDto.FromEntity(existingEvents));

        _eventRepositoryMock.Verify(repo => repo.GetTopEventsBySalesPercentageAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheServiceMock.Verify(repo => repo.GetAsync<IReadOnlyList<EventCacheDto>>(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIdNonExistingInCache_ShouldReturnFromRepositoryAndCached()
    {
        // Arrange
        var existingEvents = Enumerable.Range(0, 10).Select(x => TestEventFactory.Create()).ToList();
        var existingEventsCacheDto = EventCacheDto.FromEntity(existingEvents);
        var key = CacheKeys.Top10Events;
        var query = new GetTopEventsQuery();

        _cacheServiceMock
            .Setup(cache => cache.GetAsync<IReadOnlyList<EventCacheDto>>(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EventCacheDto>?)null);
        _eventRepositoryMock
            .Setup(repo => repo.GetTopEventsBySalesPercentageAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvents);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(existingEvents.Count);
        result.Should().BeEquivalentTo(EventResponseDto.FromEntity(existingEvents));

        _eventRepositoryMock.Verify(repo => repo.GetTopEventsBySalesPercentageAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(cache => cache.SetAsync(key, It.IsAny<IReadOnlyList<EventCacheDto>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsEmpty_ShouldNotSaveToCacheAndReturnEmptyList()
    {
        // Arrange
        var key = CacheKeys.Top10Events;
        var query = new GetTopEventsQuery();

        _cacheServiceMock
            .Setup(cache => cache.GetAsync<IReadOnlyList<EventCacheDto>>(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EventCacheDto>?)null);
        _eventRepositoryMock
            .Setup(repo => repo.GetTopEventsBySalesPercentageAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Event>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        _eventRepositoryMock.Verify(repo => repo.GetTopEventsBySalesPercentageAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(cache => cache.SetAsync(key, It.IsAny<IReadOnlyList<EventCacheDto>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var query = new GetTopEventsQuery();
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _cacheServiceMock
            .Setup(cache => cache.GetAsync<IReadOnlyList<EventCacheDto>>(CacheKeys.Top10Events, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(query, cancellationToken);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

