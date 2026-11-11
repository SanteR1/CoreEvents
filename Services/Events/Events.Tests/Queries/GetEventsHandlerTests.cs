using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using Events.Application.Abstractions.Repositories;
using Events.Application.DTOs;
using Events.Application.Queries;
using Events.Domain.Entities;
using Events.Tests.Infrastructure;
using Moq;
using Xunit;

namespace Events.Tests.Queries;

public class GetEventsHandlerTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock = new();
    private readonly GetEventsQueryHandler _handler;

    public GetEventsHandlerTests()
    {
        _handler = new GetEventsQueryHandler(_eventRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithNullDates_ShouldPassNullDatesToRepository()
    {
        // Arrange
        var eventFilter = new EventFilter
        {
            From = null,
            To = null
        };
        var query = new GetEventsQuery(eventFilter);

        _eventRepositoryMock
            .Setup(repo => repo.GetAllAsync(It.IsAny<EventFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<Event>
            {
                CurrentPage = 1,
                Items = [],
                PageSize = 10,
                TotalCount = 1
            });

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _eventRepositoryMock.Verify(
            x => x.GetAllAsync(
                It.Is<EventFilter>(f => f.From == null && f.To == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullTitle_ShouldPassNullTitleToRepository()
    {
        // Arrange
        var eventFilter = new EventFilter
        {
            Title = null
        };
        var query = new GetEventsQuery(eventFilter);

        _eventRepositoryMock
            .Setup(repo => repo.GetAllAsync(It.IsAny<EventFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<Event>
            {
                CurrentPage = 1,
                Items = [],
                PageSize = 10,
                TotalCount = 1
            });

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _eventRepositoryMock.Verify(
            x => x.GetAllAsync(
                It.Is<EventFilter>(f => f.Title == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithPageAndPageSize_ShouldPassPaginationParameters()
    {
        // Arrange
        var eventFilter = new EventFilter
        {
            PageSize = 15,
            Page = 2
        };
        var query = new GetEventsQuery(eventFilter);

        _eventRepositoryMock
            .Setup(repo => repo.GetAllAsync(It.IsAny<EventFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<Event>
            {
                CurrentPage = 2,
                Items = [],
                PageSize = 10,
                TotalCount = 15
            });

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _eventRepositoryMock.Verify(
            x => x.GetAllAsync(
                It.Is<EventFilter>(f => f.Page == 2 && f.PageSize == 15),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [MemberData(nameof(GetDateNormalizationCases))]
    public async Task Handle_ShouldPassUtcDatesToRepository(
        DateTime? inputFrom, DateTime? inputTo,
        DateTime? expectedFrom, DateTime? expectedTo)
    {
        // Arrange
        var eventFilter = new EventFilter
        {
            From = inputFrom,
            To = inputTo
        };
        var query = new GetEventsQuery(eventFilter);

        _eventRepositoryMock
            .Setup(repo => repo.GetAllAsync(It.IsAny<EventFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<Event>
            {
                CurrentPage = 1,
                Items = [],
                PageSize = 10,
                TotalCount = 1
            });

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _eventRepositoryMock.Verify(
            x => x.GetAllAsync(
                It.Is<EventFilter>(f =>
                    f.From == expectedFrom &&
                    f.To == expectedTo &&
                    (f.From == null || f.From.Value.Kind == DateTimeKind.Utc) &&
                    (f.To == null || f.To.Value.Kind == DateTimeKind.Utc)
                ),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Хэндлер передал в репозиторий некорректно нормализованные даты.");
    }

    public static TheoryData<DateTime?, DateTime?, DateTime?, DateTime?> GetDateNormalizationCases()
    {
        var data = new TheoryData<DateTime?, DateTime?, DateTime?, DateTime?>();

        // Кейс 1: Локальное время. Из From ничего не вычитаем, To без времени -> + 1 день
        var localFrom = new DateTime(2026, 6, 8, 15, 30, 0, DateTimeKind.Local);
        var localTo = new DateTime(2026, 6, 8, 0, 0, 0, DateTimeKind.Local);
        data.Add(
            localFrom,
            localTo,
            localFrom.ToUniversalTime(),
            localTo.ToUniversalTime().AddDays(1)
        );

        // Кейс 2: Unspecified (например, из Web API). From без времени, To с временем
        var unspecFrom = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Unspecified);
        var unspecTo = new DateTime(2026, 6, 10, 18, 45, 0, DateTimeKind.Unspecified);
        data.Add(
            unspecFrom,
            unspecTo,
            DateTime.SpecifyKind(unspecFrom, DateTimeKind.Utc),
            DateTime.SpecifyKind(unspecTo.AddMicroseconds(1), DateTimeKind.Utc)
        );

        // Кейс 3: Уже UTC (ничего не должно сломаться)
        var utcFrom = new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        data.Add(
            utcFrom,
            null,
            utcFrom,
            null
        );

        return data;
    }

    [Fact]
    public async Task Handle_WithValidEvent_ShouldMapEntitiesToDtos()
    {
        // Arrange
        var existEvent = TestEventFactory.Create(
            "Main Events 1",
            description: "Description Main Events",
            startAt: DateTime.UtcNow.AddDays(1).AddHours(10),
            endAt: DateTime.UtcNow.AddDays(1).AddHours(12)
        );

        var eventFilter = new EventFilter();
        var query = new GetEventsQuery(eventFilter);

        var expectedPagination = new PaginatedResult<Event>
        {
            CurrentPage = 1,
            PageSize = 10,
            TotalCount = 1,
            Items = [existEvent]
        };

        _eventRepositoryMock
            .Setup(repo => repo.GetAllAsync(It.IsAny<EventFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagination);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.CurrentPage.Should().Be(1);
        result.TotalPages.Should().Be(1);
        result.PageSize.Should().Be(10);
        var item = result.Items.Should().ContainSingle().Which;
        item.Id.Should().Be(existEvent.Id);
        item.Title.Should().Be(existEvent.Title);
        item.Description.Should().Be(existEvent.Description);
        item.AvailableSeats.Should().Be(existEvent.AvailableSeats);
        item.EndAt.Should().Be(existEvent.EndAt);
        item.StartAt.Should().Be(existEvent.StartAt);
        item.TotalSeats.Should().Be(existEvent.TotalSeats);

        _eventRepositoryMock.Verify(repo => repo.GetAllAsync(It.IsAny<EventFilter>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoEvents_ReturnsEmptyArray()
    {
        // Arrange
        var filter = new EventFilter();
        var query = new GetEventsQuery(filter);
        var expectedPagination = new PaginatedResult<Event>
        {
            CurrentPage = 1,
            PageSize = 10,
            TotalCount = 0,
            Items = []
        };

        _eventRepositoryMock
            .Setup(repo => repo.GetAllAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagination);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.PageSize.Should().Be(10);
        result.CurrentPage.Should().Be(1);

        _eventRepositoryMock.Verify(repo => repo.GetAllAsync(filter, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldPassTokenToRepository()
    {
        // Arrange
        var eventFilter = new EventFilter();
        var query = new GetEventsQuery(eventFilter);
        var expectedPagination = new PaginatedResult<Event>
        {
            CurrentPage = 1,
            PageSize = 10,
            TotalCount = 0,
            Items = []
        };
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _eventRepositoryMock.Setup(repo => repo.GetAllAsync(eventFilter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPagination);

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _eventRepositoryMock.Verify(
            repo => repo.GetAllAsync(eventFilter, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancellationRequested_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var eventFilter = new EventFilter();
        var query = new GetEventsQuery(eventFilter);
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _eventRepositoryMock.Setup(repo => repo.GetAllAsync(eventFilter, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(query, cancellationToken);
        await act.Should().ThrowAsync<OperationCanceledException>();

        _eventRepositoryMock.Verify(
            repo => repo.GetAllAsync(eventFilter, cancellationToken),
            Times.Once);
    }

    [Theory]
    [InlineData(-1, "Значение должно быть в диапазоне от 1 до 100000")]
    [InlineData(0, "Значение должно быть в диапазоне от 1 до 100000")]
    [InlineData(100001, "Значение должно быть в диапазоне от 1 до 100000")]
    public void EventFilter_InvalidPagePagination_ShouldReturnValidationError(int page, string expectedError)
    {
        // Arrange
        var filter = new EventFilter { Page = page };

        // Act
        var context = new ValidationContext(filter);
        var results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(filter, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().ContainSingle(r => r.ErrorMessage == expectedError);
    }

    [Theory]
    [InlineData(0, "Значение должно быть в диапазоне от 1 до 100")]
    [InlineData(-1, "Значение должно быть в диапазоне от 1 до 100")]
    [InlineData(101, "Значение должно быть в диапазоне от 1 до 100")]
    public void EventFilter_InvalidPageSizePagination_ShouldReturnValidationError(int pageSize, string expectedError)
    {
        // Arrange
        var filter = new EventFilter { PageSize = pageSize };

        // Act
        var context = new ValidationContext(filter);
        var results = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(filter, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().ContainSingle(r => r.ErrorMessage == expectedError);
    }
}

