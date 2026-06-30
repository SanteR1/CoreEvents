using System.ComponentModel.DataAnnotations;
using CoreEvents.Application.DTOs;
using CoreEvents.Application.Interfaces.Repositories;
using CoreEvents.Application.Services;
using CoreEvents.Domain.Entities;
using CoreEvents.Domain.Exceptions;
using CoreEvents.Tests.Infrastructure;
using FluentAssertions;
using Moq;

namespace CoreEvents.Tests.Services
{
    public class EventServiceTests
    {
        private readonly Mock<IEventRepository> _eventRepositoryMock;
        private readonly EventService _eventService;

        public EventServiceTests()
        {
            _eventRepositoryMock = new Mock<IEventRepository>();
            _eventService = new EventService(_eventRepositoryMock.Object);
        }

        #region CreateEventAsync Tests
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateEventAsync_WithEmptyOrNullTitle_ShouldThrowsValidationException(string? title)
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var createEvent = new EventCreateDto(
                Title: title,
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10);

            // Act & Assert
            Func<Task> act = async () => await _eventService.CreateEventAsync(createEvent, TestContext.Current.CancellationToken);
            var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();
            exceptionAssertion.Which.ValidationResult.MemberNames.Should().Contain("title");

            _eventRepositoryMock.Verify(repo => repo.Add(It.IsAny<Event>()), Times.Never);
            _eventRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateEventAsync_WithEndAtBeforeStartAt_ShouldThrowsValidationException()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var createEventDto = new EventCreateDto(
                Title: "Test Event",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(-1),
                TotalSeats: 10);

            // Act & Assert
            Func<Task> act = async () => await _eventService.CreateEventAsync(createEventDto);
            var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();
            exceptionAssertion.Which.ValidationResult.MemberNames.Should().Contain("endAt");

            _eventRepositoryMock.Verify(repo => repo.Add(It.IsAny<Event>()), Times.Never);
            _eventRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateEventAsync_WithEndAtEqualToStartAt_ShouldThrowsValidationException()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var createEvent = new EventCreateDto(
                Title: "Test Event",
                StartAt: futureDate,
                EndAt: futureDate,
                TotalSeats: 10);

            // Act & Assert
            Func<Task> act = async () => await _eventService.CreateEventAsync(createEvent);
            var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();
            exceptionAssertion.Which.ValidationResult.MemberNames.Should().Contain("startAt").And.Contain("endAt");

            _eventRepositoryMock.Verify(repo => repo.Add(It.IsAny<Event>()), Times.Never);
            _eventRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateEventAsync_WithInvalidDates_ShouldThrowsValidationException()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var createEvent = new EventCreateDto(
                Title: "Create Title",
                Description: "Create Description",
                StartAt: futureDate,
                EndAt: futureDate.AddDays(-1),
                TotalSeats: 1);

            // Act & Assert
            Func<Task> act = async () => await _eventService.CreateEventAsync(createEvent);
            var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();
            exceptionAssertion.Which.ValidationResult.MemberNames.Should().Contain("endAt");

            _eventRepositoryMock.Verify(repo => repo.Add(It.IsAny<Event>()), Times.Never);
            _eventRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateEventAsync_WithPastStartAt_ShouldThrowsValidationException()
        {
            // Arrange
            var pastDate = DateTime.UtcNow.AddDays(-1);
            var createEvent = new EventCreateDto(
                Title: "Test Event",
                StartAt: pastDate,
                EndAt: pastDate.AddHours(2),
                TotalSeats: 10);

            // Act & Assert
            Func<Task> act = async () => await _eventService.CreateEventAsync(createEvent);
            var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();
            exceptionAssertion.Which.ValidationResult.MemberNames.Should().Contain("startAt");

            _eventRepositoryMock.Verify(repo => repo.Add(It.IsAny<Event>()), Times.Never);
            _eventRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateEventAsync_WithTitleWhitespace_TrimsTitleAndCreatesEvent()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var createEvent = new EventCreateDto(
                Title: "  Test Event  ",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10);

            // Act
            var result = await _eventService.CreateEventAsync(createEvent, TestContext.Current.CancellationToken);

            // Assert
            result.Title.Should().Be("Test Event");
            _eventRepositoryMock.Verify(repo => repo.Add(It.IsAny<Event>()), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateEventAsync_WithValidData_ShouldReturnCreatedEvent()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var dto = new EventCreateDto(
                Title: "Title",
                Description: "Desc",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10);

            // Act
            var result = await _eventService.CreateEventAsync(dto, TestContext.Current.CancellationToken);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBe(Guid.Empty);
            result.Title.Should().Be("Title");
            result.Description.Should().Be("Desc");
            result.StartAt.Should().Be(futureDate);
            result.EndAt.Should().Be(futureDate.AddHours(2));
            result.TotalSeats.Should().Be(dto.TotalSeats);

            _eventRepositoryMock.Verify(repo => repo.Add(It.IsAny<Event>()), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateEventAsync_WhenValid_ShouldPassTokenToRepository()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var dto = new EventCreateDto(
                Title: "Title",
                Description: "Desc",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10);
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            // Act
            await _eventService.CreateEventAsync(dto, cancellationToken);

            // Assert
            _eventRepositoryMock.Verify(
                repo => repo.SaveChangesAsync(cancellationToken),
                Times.Once,
                "Сервис забыл прокинуть CancellationToken в репозиторий!");
        }

        [Fact]
        public async Task CreateEventAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
        {
            // Arrange
            string expectedExceptionMessage = $"The operation was canceled.";
            var futureDate = DateTime.UtcNow.AddDays(1);
            var dto = new EventCreateDto(
                Title: "Title",
                Description: "Desc",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var cancellationToken = cts.Token;

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            Func<Task> act = async () => await _eventService.CreateEventAsync(dto, cancellationToken);
            await act.Should().ThrowAsync<OperationCanceledException>().WithMessage(expectedExceptionMessage);

            _eventRepositoryMock.Verify(
                repo => repo.SaveChangesAsync(cancellationToken),
                Times.Once);
        }
        #endregion

        #region DeleteEventAsync Tests

        [Fact]
        public async Task DeleteEventAsync_WithExistingId_ShouldRemoveEventAndReturnTrue()
        {
            // Arrange
            var existEvent = TestEventFactory.Create();

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existEvent);

            // Act
            var result = await _eventService.DeleteEventAsync(existEvent.Id, TestContext.Current.CancellationToken);

            result.Should().BeTrue();
            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.Delete(existEvent), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteEventAsync_WithInvalidId_ShouldThrowNotFoundException()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            string expectedExceptionMessage = $"Событие с ID {invalidId} не найдено.";

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Event?)null);

            // Act & Assert
            Func<Task> act = async () => await _eventService.DeleteEventAsync(invalidId, TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<NotFoundException>().WithMessage(expectedExceptionMessage);
            
            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.Delete(It.IsAny<Event>()), Times.Never);
            _eventRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
        [Fact]
        public async Task DeleteEventAsync_WhenValid_ShouldPassTokenToRepository()
        {
            // Arrange
            var existEvent = TestEventFactory.Create();
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existEvent);

            // Act
            await _eventService.DeleteEventAsync(existEvent.Id, cancellationToken);

            // Assert
            _eventRepositoryMock.Verify(
                repo => repo.GetByIdAsync(existEvent.Id, cancellationToken),
                Times.Once,
                "Сервис забыл прокинуть CancellationToken в репозиторий!");
        }

        [Fact]
        public async Task DeleteEventAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var existEvent = TestEventFactory.Create();
            string expectedExceptionMessage = $"The operation was canceled.";
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            Func<Task> act = async () => await _eventService.DeleteEventAsync(existEvent.Id, cancellationToken);
            await act.Should().ThrowAsync<OperationCanceledException>().WithMessage(expectedExceptionMessage);

            _eventRepositoryMock.Verify(
                repo => repo.GetByIdAsync(existEvent.Id, cancellationToken),
                Times.Once);
        }
        #endregion

        #region GetAllEventsAsync

        [Fact]
        public async Task GetAllEventsAsync_WithNullDates_ShouldPassNullDatesToRepository()
        {
            // Arrange
            EventFilter eventFilter = new EventFilter()
            {
                From = null,
                To = null
            };

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetAllAsync(eventFilter, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaginatedResult<Event>()
                {
                    CurrentPage = 1,
                    Items = [],
                    PageSize = 10,
                    TotalCount = 1
                });

            // Act
            await _eventService.GetAllEventsAsync(eventFilter, TestContext.Current.CancellationToken);

            // Assert
            _eventRepositoryMock.Verify(
                x => x.GetAllAsync(
                    It.Is<EventFilter>(f =>
                        f.From == null && f.To == null),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllEventsAsync_WithNullTitle_ShouldPassNullTitleToRepository()
        {
            // Arrange
            EventFilter eventFilter = new EventFilter()
            {
                Title = null
            };

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetAllAsync(eventFilter, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaginatedResult<Event>()
                {
                    CurrentPage = 1,
                    Items = [],
                    PageSize = 10,
                    TotalCount = 1
                });

            // Act
            await _eventService.GetAllEventsAsync(eventFilter, TestContext.Current.CancellationToken);

            // Assert
            _eventRepositoryMock.Verify(
                x => x.GetAllAsync(
                    It.Is<EventFilter>(f =>
                        f.Title == null),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAllEventsAsync_WithPageAndPageSize_ShouldPassPaginationParameters()
        {
            // Arrange
            EventFilter eventFilter = new EventFilter()
            {
                PageSize = 15,
                Page = 2
            };

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetAllAsync(eventFilter, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaginatedResult<Event>()
                {
                    CurrentPage = 2,
                    Items = [],
                    PageSize = 10,
                    TotalCount = 15
                });

            // Act
            await _eventService.GetAllEventsAsync(eventFilter, TestContext.Current.CancellationToken);

            // Assert
            _eventRepositoryMock.Verify(
                x => x.GetAllAsync(
                    It.Is<EventFilter>(f =>
                        f.Page == 2 && f.PageSize == 15),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [MemberData(nameof(GetDateNormalizationCases))]
        public async Task GetAllEventsAsync_ShouldPassUtcDatesToRepository(
            DateTime? inputFrom, DateTime? inputTo,
            DateTime? expectedFrom, DateTime? expectedTo)
        {
            // Arrange
            EventFilter eventFilter = new EventFilter()
            {
                From = inputFrom,
                To = inputTo
            };

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetAllAsync(It.IsAny<EventFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaginatedResult<Event>()
                {
                    CurrentPage = 1,
                    Items = [],
                    PageSize = 10,
                    TotalCount = 1
                });

            // Act
            await _eventService.GetAllEventsAsync(eventFilter, TestContext.Current.CancellationToken);

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
                "Сервис передал в репозиторий некорректно нормализованные даты.");
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
                localFrom.ToUniversalTime(), // Ожидаем честную конвертацию в UTC
                localTo.ToUniversalTime().AddDays(1) // Ожидаем конвертацию и сдвиг на день
            );

            // Кейс 2: Unspecified (например, из Web API). From без времени, To с временем
            var unspecFrom = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Unspecified);
            var unspecTo = new DateTime(2026, 6, 10, 18, 45, 0, DateTimeKind.Unspecified);
            data.Add(
                unspecFrom,
                unspecTo,
                DateTime.SpecifyKind(unspecFrom, DateTimeKind.Utc), // Ожидаем просто ярлык UTC
                DateTime.SpecifyKind(unspecTo.AddMicroseconds(1), DateTimeKind.Utc) // Ожидаем ярлык и +1 микросекунду
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
        public async Task GetAllEventsAsync_WithValidEvent_ShouldMapEntitiesToDtos()
        {
            // Arrange
            var existEvent = TestEventFactory.Create(
                "Main Events 1",
                description: "Description Main Events",
                startAt: DateTime.UtcNow.AddDays(1).AddHours(10),
                endAt: DateTime.UtcNow.AddDays(1).AddHours(12)
                );

            EventFilter eventFilter = new EventFilter();

            var expectedPagination = new PaginatedResult<Event>
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 1,
                Items = [existEvent]
            };

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetAllAsync(It.IsAny<EventFilter>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedPagination);

            // Act
            var result = await _eventService.GetAllEventsAsync(eventFilter, TestContext.Current.CancellationToken);

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
        public async Task GetAllEventsAsync_WithNoEvents_ReturnsEmptyArray()
        {
            // Arrange
            var filter = new EventFilter();
            var expectedPagination = new PaginatedResult<Event>
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 0,
                Items = []
            };

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetAllAsync(filter, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedPagination);

            // Act
            var result = await _eventService.GetAllEventsAsync(filter, TestContext.Current.CancellationToken);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.PageSize.Should().Be(10);
            result.CurrentPage.Should().Be(1);

            _eventRepositoryMock.Verify(repo => repo.GetAllAsync(filter, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllEventsAsync_WhenValid_ShouldPassTokenToRepository()
        {
            // Arrange
            var eventFilter = new EventFilter();
            var expectedPagination = new PaginatedResult<Event>
            {
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 0,
                Items = []
            };
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            // Setup
            _eventRepositoryMock.Setup(repo => repo.GetAllAsync(eventFilter, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedPagination);

            // Act
            await _eventService.GetAllEventsAsync(eventFilter, cancellationToken);

            // Assert
            _eventRepositoryMock.Verify(
                repo => repo.GetAllAsync(eventFilter, cancellationToken),
                Times.Once,
                "Сервис забыл прокинуть CancellationToken в репозиторий!");
        }

        [Fact]
        public async Task GetAllEventsAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var eventFilter = new EventFilter();
            string expectedExceptionMessage = $"The operation was canceled.";
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            // Setup
            _eventRepositoryMock.Setup(repo => repo.GetAllAsync(eventFilter, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            Func<Task> act = async () => await _eventService.GetAllEventsAsync(eventFilter, cancellationToken);
            await act.Should().ThrowAsync<OperationCanceledException>().WithMessage(expectedExceptionMessage);

            _eventRepositoryMock.Verify(
                repo => repo.GetAllAsync(eventFilter, cancellationToken),
                Times.Once);
        }
        #endregion

        #region GetAllEventsAsync Validation Tests
        [Theory]
        [InlineData(-1, "Значение должно быть в диапазоне от 1 до 100000")]
        [InlineData(0, "Значение должно быть в диапазоне от 1 до 100000")]
        [InlineData(100001, "Значение должно быть в диапазоне от 1 до 100000")]
        public void GetAllEventsAsync_InvalidPagePagination_ShouldReturnValidationError(int page, string expectedError)
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
        public void GetAllEventsAsync_InvalidPageSizePagination_ShouldReturnValidationError(int pageSize, string expectedError)
        {
            // Arrange
            var filter = new EventFilter() { PageSize = pageSize };

            // Act
            var context = new ValidationContext(filter);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(filter, context, results, true);

            // Assert
            isValid.Should().BeFalse();
            results.Should().ContainSingle(r => r.ErrorMessage == expectedError);
        }

        #endregion

        #region GetEventByIdAsync
        [Fact]
        public async Task GetEventByIdAsync_ExistingId_ShouldRetrieveEventSuccessfully()
        {
            // Arrange
            var existingEvent = TestEventFactory.Create();

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEvent);

            // Act
            var result = await _eventService.GetEventByIdAsync(existingEvent.Id, TestContext.Current.CancellationToken);

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
        }

        [Fact]
        public async Task GetEventByIdAsync_NonExisting_ShouldThrowNotFoundException()
        {
            // Arrange
            Guid expectedId = Guid.NewGuid();
            string expectedExceptionMessage = $"Событие с ID {expectedId} не найдено.";

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(expectedId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Event?)null);

            // Act & Assert
            Func<Task> act = async () => await _eventService.GetEventByIdAsync(expectedId);
            await act.Should().ThrowAsync<NotFoundException>().WithMessage(expectedExceptionMessage);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(expectedId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetEventByIdAsync_WhenValid_ShouldPassTokenToRepository()
        {
            // Arrange
            var existEvent = TestEventFactory.Create();
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            // Setup
            _eventRepositoryMock.Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existEvent);

            // Act
            await _eventService.GetEventByIdAsync(existEvent.Id, cancellationToken);

            // Assert
            _eventRepositoryMock.Verify(
                repo => repo.GetByIdAsync(existEvent.Id, cancellationToken),
                Times.Once,
                "Сервис забыл прокинуть CancellationToken в репозиторий!");
        }

        [Fact]
        public async Task GetEventByIdAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var existEvent = TestEventFactory.Create();
            string expectedExceptionMessage = $"The operation was canceled.";
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            // Setup
            _eventRepositoryMock.Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            Func<Task> act = async () => await _eventService.GetEventByIdAsync(existEvent.Id, cancellationToken);
            await act.Should().ThrowAsync<OperationCanceledException>().WithMessage(expectedExceptionMessage);

            _eventRepositoryMock.Verify(
                repo => repo.GetByIdAsync(existEvent.Id, cancellationToken),
                Times.Once);
        }
        #endregion

        #region UpdateEventAsync
        [Fact]
        public async Task UpdateEventAsync_NonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            Guid expectedId = Guid.NewGuid();
            string expectedExceptionMessage = $"Событие с ID {expectedId} не найдено.";
            var futureDate = DateTime.UtcNow.AddDays(1);

            EventUpdateDto entityDto = new EventUpdateDto(
                Title: "Update Title",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                Description: "Update Description");

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(expectedId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Event?)null);

            // Act & Assert
            Func<Task> act = async () => await _eventService.UpdateEventAsync(expectedId, entityDto);
            await act.Should().ThrowAsync<NotFoundException>().WithMessage(expectedExceptionMessage);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(expectedId, It.IsAny<CancellationToken>()), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.Update(It.IsAny<Event>()), Times.Never);
            _eventRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateEventAsync_WithExistingId_ShouldModifyEvent()
        {
            // Arrange
            var existingEvent = TestEventFactory.Create();

            var updateDto = new EventUpdateDto(
                Title: "Title Update",
                Description: "Updated Description",
                StartAt: DateTime.UtcNow.AddDays(2),
                EndAt: DateTime.UtcNow.AddDays(3));

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEvent);

            // Act
            var result = await _eventService.UpdateEventAsync(existingEvent.Id, updateDto, TestContext.Current.CancellationToken);

            // Assert Mapping
            result.Id.Should().Be(existingEvent.Id);
            result.Title.Should().Be(updateDto.Title);
            result.Description.Should().Be(updateDto.Description);
            result.StartAt.Should().Be(updateDto.StartAt);
            result.EndAt.Should().Be(updateDto.EndAt);
            result.TotalSeats.Should().Be(10);
            result.AvailableSeats.Should().Be(10);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateEventAsync_WithEndAtBeforeStartAt_ShouldThrowValidationException()
        {
            // Arrange
            var existingEvent = TestEventFactory.Create();
            var updateDto = new EventUpdateDto(
                Title: "Update Title",
                Description: "Update Description",
                StartAt: DateTime.UtcNow.AddDays(1),
                EndAt: DateTime.UtcNow.AddDays(-1));

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEvent);

            // Act & Assert
            Func<Task> act = async () => await _eventService.UpdateEventAsync(existingEvent.Id, updateDto);
            var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();
            exceptionAssertion.Which.ValidationResult.MemberNames.Should().Contain("endAt");

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateEventAsync_WithNullTitle_ThrowsValidationException()
        {
            // Arrange
            var existingEvent = TestEventFactory.Create();
            var updateDto = new EventUpdateDto
            (
                Title: null,
                StartAt: DateTime.UtcNow.AddDays(1),
                EndAt: DateTime.UtcNow.AddDays(1).AddHours(2));

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEvent);

            // Act & Assert
            Func<Task> act = async () => await _eventService.UpdateEventAsync(existingEvent.Id, updateDto);
            var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();
            exceptionAssertion.Which.ValidationResult.MemberNames.Should().Contain("title");

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateEventAsync_WithPastStartAt_ThrowsValidationException()
        {
            // Arrange
            var existingEvent = TestEventFactory.Create();

            var pastDate = DateTime.UtcNow.AddDays(-1);
            var updateDto = new EventUpdateDto(
                Title: "Updated Event",
                StartAt: pastDate,
                EndAt: pastDate.AddHours(2));

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEvent);

            // Act & Assert
            Func<Task> act = async () => await _eventService.UpdateEventAsync(existingEvent.Id, updateDto);
            var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();
            exceptionAssertion.Which.ValidationResult.MemberNames.Should().Contain("startAt");

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateEventAsync_WithValidData_UpdatesEvent()
        {
            // Arrange
            var existingEvent = TestEventFactory.Create();

            var newFutureDate = DateTime.UtcNow.AddDays(2);
            var updateEvent = new EventUpdateDto(
                Title: "Updated Event",
                Description: "Updated Description",
                StartAt: newFutureDate,
                EndAt: newFutureDate.AddHours(3));

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEvent);

            // Act
            var result = await _eventService.UpdateEventAsync(existingEvent.Id, updateEvent, TestContext.Current.CancellationToken);

            // Assert
            result.Id.Should().Be(existingEvent.Id);
            result.Title.Should().Be(updateEvent.Title);
            result.Description.Should().Be(updateEvent.Description);
            result.StartAt.Should().Be(newFutureDate);
            result.EndAt.Should().Be(newFutureDate.AddHours(3));
            
            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(existingEvent.Id, It.IsAny<CancellationToken>()), Times.Once);
            _eventRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateEventAsync_WhenValid_ShouldPassTokenToRepository()
        {
            // Arrange
            var existEvent = TestEventFactory.Create();
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var updateEvent = new EventUpdateDto(
                Title: "Updated Event",
                Description: "Updated Description",
                StartAt: DateTime.UtcNow.AddDays(2),
                EndAt: DateTime.UtcNow.AddDays(2).AddHours(3));

            // Setup
            _eventRepositoryMock.Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existEvent);

            // Act
            await _eventService.UpdateEventAsync(existEvent.Id, updateEvent, cancellationToken);

            // Assert
            _eventRepositoryMock.Verify(
                repo => repo.GetByIdAsync(existEvent.Id, cancellationToken),
                Times.Once,
                "Сервис забыл прокинуть CancellationToken в репозиторий!");
        }

        [Fact]
        public async Task UpdateEventAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var existEvent = TestEventFactory.Create();
            string expectedExceptionMessage = $"The operation was canceled.";
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var updateEvent = new EventUpdateDto(
                Title: "Updated Event",
                Description: "Updated Description",
                StartAt: DateTime.UtcNow.AddDays(2),
                EndAt: DateTime.UtcNow.AddDays(2).AddHours(3));

            // Setup
            _eventRepositoryMock.Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            Func<Task> act = async () =>
                await _eventService.UpdateEventAsync(existEvent.Id, updateEvent, cancellationToken);
            await act.Should().ThrowAsync<OperationCanceledException>().WithMessage(expectedExceptionMessage);

            _eventRepositoryMock.Verify(
                repo => repo.GetByIdAsync(existEvent.Id, cancellationToken),
                Times.Once);
        }
        #endregion
    }
}