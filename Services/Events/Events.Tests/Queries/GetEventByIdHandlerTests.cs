using AwesomeAssertions;
using Events.Application.Abstractions.Caching;
using Events.Application.Abstractions.Repositories;
using Events.Application.DTOs;
using Events.Application.Exceptions;
using Events.Application.Queries;
using Events.Domain.Entities;
using Events.Tests.Infrastructure;
using Moq;
using Xunit;

namespace Events.Tests.Queries;

public class GetEventByIdHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly GetEventByIdQueryHandler _handler;

    public GetEventByIdHandlerTests()
    {
        _handler = new GetEventByIdQueryHandler(_eventRepositoryMock.Object, _cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingId_ShouldRetrieveEventSuccessfully()
    {
        // Arrange
        var existingEvent = TestEventFactory.Create();
        var query = new GetEventByIdQuery(existingEvent.Id);

        _cacheServiceMock
            .Setup(c => c.GetAsync<EventCacheDto>(CacheKeys.Event(existingEvent.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventCacheDto?)null);
        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(existingEvent.Id);
        result.Title.Should().Be(existingEvent.Title);
        result.Description.Should().Be(existingEvent.Description);
        result.StartAt.Should().Be(existingEvent.StartAt);
        result.EndAt.Should().Be(existingEvent.EndAt);
        result.TotalSeats.Should().Be(existingEvent.TotalSeats);
        result.AvailableSeats.Should().Be(existingEvent.AvailableSeats);

        _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(c => c.SetAsync(CacheKeys.Event(existingEvent.Id), It.IsAny<EventCacheDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExisting_ShouldThrowEventNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        var query = new GetEventByIdQuery(nonExistingId);

        _cacheServiceMock
            .Setup(c => c.GetAsync<EventCacheDto>(CacheKeys.Event(nonExistingId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventCacheDto?)null);
        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(nonExistingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);
        var exceptionAssertion = await act.Should().ThrowAsync<EventNotFoundException>();

        exceptionAssertion.Which.ErrorCode.Should().Be("Event.NotFound");
        exceptionAssertion.Which.ErrorData.Should().BeEquivalentTo(new { parameter = "Id", value = nonExistingId.ToString() });

        _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(nonExistingId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldPassTokenToRepository()
    {
        // Arrange
        var existingEvent = TestEventFactory.Create();
        var query = new GetEventByIdQuery(existingEvent.Id);
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _cacheServiceMock
            .Setup(c => c.GetAsync<EventCacheDto>(CacheKeys.Event(existingEvent.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventCacheDto?)null);
        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _eventRepositoryMock.Verify(
            repo => repo.GetByIdAsync(existingEvent.Id, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var existingEvent = TestEventFactory.Create();
        var query = new GetEventByIdQuery(existingEvent.Id);
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _cacheServiceMock
            .Setup(c => c.GetAsync<EventCacheDto>(CacheKeys.Event(existingEvent.Id), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(query, cancellationToken);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Handle_WhenIdExistingInCache_ShouldReturnFromCacheWithoutHittingRepository()
    {
        // Arrange
        var existingEvent = TestEventFactory.Create();
        var cachedDto = EventCacheDto.FromEntity(existingEvent);
        var query = new GetEventByIdQuery(existingEvent.Id);

        _cacheServiceMock
            .Setup(c => c.GetAsync<EventCacheDto>(CacheKeys.Event(existingEvent.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(existingEvent.Id);
        result.Title.Should().Be(existingEvent.Title);

        _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _cacheServiceMock.Verify(c => c.GetAsync<EventCacheDto>(CacheKeys.Event(existingEvent.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIdNonExistingInCache_ShouldReturnFromRepositoryAndCacheResult()
    {
        // Arrange
        var existingEvent = TestEventFactory.Create();
        var key = CacheKeys.Event(existingEvent.Id);
        var query = new GetEventByIdQuery(existingEvent.Id);

        _cacheServiceMock
            .Setup(c => c.GetAsync<EventCacheDto>(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventCacheDto?)null);
        _eventRepositoryMock
            .Setup(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvent);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(existingEvent.Id);

        _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(c => c.SetAsync(key, It.Is<EventCacheDto>(dto => dto.Id == existingEvent.Id), It.IsAny<CancellationToken>()), Times.Once);
    }
}
