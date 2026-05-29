using CoreEvents.Data.DataAccess;
using CoreEvents.Models.DTOs;
using CoreEvents.Services.Implementations;
using CoreEvents.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using CoreEvents.Middleware;

namespace CoreEvents.Tests.Services
{
    public class EventServiceTests : IDisposable
    {
        private readonly IEventService _eventService;
        private readonly IServiceScope _scope;
        private readonly ServiceProvider _serviceProvider;

        public EventServiceTests()
        {
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
            services.AddScoped<IEventService, EventService>();

            _serviceProvider = services.BuildServiceProvider();
            _scope = _serviceProvider.CreateScope();
            _eventService = _scope.ServiceProvider.GetRequiredService<IEventService>();
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

            // Act
            var exception = await Assert.ThrowsAsync<ValidationException>(() => _eventService.CreateEventAsync(createEvent));

            // Assert
            Assert.Contains("title", exception.ValidationResult.MemberNames);
        }

        [Fact]
        public async Task CreateEventAsync_WithEndAtBeforeStartAt_ShouldThrowsValidationException()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var createEvent = new EventCreateDto(
                Title: "Test Event",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(-1),
                TotalSeats: 10);

            // Act
            var exception = await Assert.ThrowsAsync<ValidationException>(() => _eventService.CreateEventAsync(createEvent));

            // Assert
            Assert.Contains("endAt", exception.ValidationResult.MemberNames);
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

            // Act
            var exception = await Assert.ThrowsAsync<ValidationException>(() => _eventService.CreateEventAsync(createEvent));

            // Assert
            Assert.Contains("startAt", exception.ValidationResult.MemberNames);
            Assert.Contains("endAt", exception.ValidationResult.MemberNames);
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

            // Act
            var exception = await Assert.ThrowsAsync<ValidationException>(() => _eventService.CreateEventAsync(createEvent));

            // Assert
            Assert.Contains("endAt", exception.ValidationResult.MemberNames);
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

            // Act
            var exception = await Assert.ThrowsAsync<ValidationException>(() => _eventService.CreateEventAsync(createEvent));

            // Assert
            Assert.Contains("startAt", exception.ValidationResult.MemberNames);
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
            var result = await _eventService.CreateEventAsync(createEvent);

            // Assert
            Assert.Equal("Test Event", result.Title);
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
            var result = await _eventService.CreateEventAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal("Title", result.Title);
            Assert.Equal("Desc", result.Description);
            Assert.Equal(futureDate, result.StartAt);
            Assert.Equal(futureDate.AddHours(2), result.EndAt);
            Assert.Equal(dto.TotalSeats, result.TotalSeats);
        }
        #endregion

        #region DeleteEventAsync Tests

        [Fact]
        public async Task DeleteEventAsync_DeletedEventCannotBeRetrieved()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var createdEvent = await _eventService.CreateEventAsync(
                new EventCreateDto(
                Title: "Event to Delete",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10));

            // Act
            await _eventService.DeleteEventAsync(createdEvent.Id);

            // Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                _eventService.GetEventByIdAsync(createdEvent.Id));
        }

        [Fact]
        public async Task DeleteEventAsync_WithExistingId_ShouldRemoveEvent()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var createdEvent = await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Title",
                Description: "Desc",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10));

            var initialCount = (await _eventService.GetAllEventsAsync(new EventFilter())).TotalCount;

            // Act
            await _eventService.DeleteEventAsync(createdEvent.Id);
            var allEventAfterDelete = await _eventService.GetAllEventsAsync(new EventFilter());

            // Assert
            Assert.DoesNotContain(allEventAfterDelete.Items, x => x.Id == createdEvent.Id);
            Assert.Equal(initialCount - 1, allEventAfterDelete.TotalCount);
        }

        [Fact]
        public async Task DeleteEventAsync_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var result = await _eventService.DeleteEventAsync(invalidId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteEventAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var createdEvent = await _eventService.CreateEventAsync(
                new EventCreateDto(
                Title: "Event to Delete",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10));

            // Act
            var result = await _eventService.DeleteEventAsync(createdEvent.Id);

            // Assert
            Assert.True(result);
        }

        #endregion

        public void Dispose()
        {
            _scope.Dispose();
            _serviceProvider.Dispose();
        }

        #region GetAllEventsAsync Filter Tests
        [Fact]
        public async Task GetAllEventsAsync_WithTitleFilter_ShouldReturnFilteredEvents()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var createdEvent1 = await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Main Events 1",
                Description: "Desc",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10));
            await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Main Events 2",
                Description: "Desc",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10));

            EventFilter eventFilter = new EventFilter() { Title = createdEvent1.Title };

            // Act
            var result = await _eventService.GetAllEventsAsync(eventFilter);

            // Assert
            Assert.Equal(1, result.TotalCount);

            var items = result.Items.ToList();
            Assert.Single(items);

            var actualEvent = items.First();
            Assert.Equal(eventFilter.Title, actualEvent.Title);
        }

        [Fact]
        public async Task GetAllEventsAsync_WithTitleFilter_ShouldReturnMatching()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Programming 1C",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10));
            await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Other",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10));
            await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Programming Python",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10));

            var filter = new EventFilter { Title = "Programming" };

            // Act
            var result = await _eventService.GetAllEventsAsync(filter);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, e => Assert.Contains("Programming", e.Title));
        }

        [Fact]
        public async Task GetAllEventsAsync_WithFromAndToFilter_ShouldIncludeAllEventsForThatDay()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var createdEvent1 = await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Event 2026 01 02",
                Description: "For City",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10));
            var createdEvent2 = await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Event 2026 01 02",
                Description: "For City",
                StartAt: futureDate.AddDays(1),
                EndAt: futureDate.AddDays(1).AddHours(2),
                TotalSeats: 10));


            int expectedCount = 1;
            EventFilter eventFilter = new EventFilter() { From = futureDate, To = futureDate.AddHours(2) };


            // Act
            var result = await _eventService.GetAllEventsAsync(eventFilter);

            // Assert
            Assert.Equal(expectedCount, result.TotalCount);

            var items = result.Items;
            Assert.Single(result.Items);

            var actualEvent = items.First();
            Assert.Equal(createdEvent1.Title, actualEvent.Title);
            Assert.Equal(createdEvent1.Description, actualEvent.Description);
            Assert.Equal(createdEvent1.StartAt, actualEvent.StartAt);
            Assert.Equal(createdEvent1.EndAt, actualEvent.EndAt);
        }

        [Fact]
        public async Task GetAllEventsAsync_WithFromFilter_ReturnsFilteredEvents()
        {
            var futureDate1 = DateTime.UtcNow.AddDays(1);
            var futureDate2 = DateTime.UtcNow.AddDays(2);
            var filterDate = futureDate1.AddHours(1);

            await _eventService.CreateEventAsync(
                new EventCreateDto(
                Title: "Event 1",
                StartAt: futureDate1,
                EndAt: futureDate1.AddHours(2),
                TotalSeats: 10));

            await _eventService.CreateEventAsync(
                new EventCreateDto(
                Title: "Event 2",
                StartAt: futureDate2,
                EndAt: futureDate2.AddHours(2),
                TotalSeats: 10));

            var result = await _eventService.GetAllEventsAsync(new EventFilter()
            {
                From = filterDate
            });

            Assert.Single(result.Items);
            Assert.Equal("Event 2", result.Items.First().Title);
        }

        [Fact]
        public async Task GetAllEventsAsync_WithToFilter_ReturnsFilteredEvents()
        {
            var futureDate1 = DateTime.UtcNow.AddDays(1);
            var futureDate2 = DateTime.UtcNow.AddDays(2);
            var filterDate = futureDate2.AddHours(1);

            await _eventService.CreateEventAsync(
                new EventCreateDto(
                    Title: "Event 1",
                    StartAt: futureDate1,
                    EndAt: futureDate1.AddHours(2),
                    TotalSeats: 10));

            await _eventService.CreateEventAsync(
                new EventCreateDto(
                    Title: "Event 2",
                    StartAt: futureDate2,
                    EndAt: futureDate2.AddHours(2),
                    TotalSeats: 10));

            var result = await _eventService.GetAllEventsAsync(new EventFilter()
            {
                To = filterDate
            });

            Assert.Single(result.Items);
            Assert.Equal("Event 1", result.Items.First().Title);
        }

        [Fact]
        public async Task GetAllEventsAsync_CombinedFilters_ShouldReturnCorrectSubset()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var targetTitle = "Target";

            // 1. Подходит под всё
            await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Target",
                Description: "D",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(5),
                TotalSeats: 10));

            // 2. Подходит по заголовку, но не по дате
            await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Target",
                Description: "D",
                StartAt: futureDate.AddDays(1),
                EndAt: futureDate.AddDays(1).AddHours(2),
                TotalSeats: 10));

            // 3. Подходит по дате, но не по заголовку
            await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Other",
                Description: "D",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(5),
                TotalSeats: 10));

            var filter = new EventFilter
            {
                Title = targetTitle,
                From = futureDate,
                To = futureDate.AddHours(5),
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = await _eventService.GetAllEventsAsync(filter);

            // Assert
            Assert.Single(result.Items);
            Assert.Equal(targetTitle, result.Items.First().Title);
            Assert.Equal(futureDate, result.Items.First().StartAt);
        }

        [Fact]
        public async Task GetAllEventsAsync_WithNoEvents_ReturnsEmptyArray()
        {
            var result = await _eventService.GetAllEventsAsync(new EventFilter());

            Assert.NotNull(result);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetAllEventsAsync_EmptyFilter_ShouldReturnAllEvents(string? emptyTitle)
        {
            var futureDate = DateTime.UtcNow.AddDays(1);
            var eventEntity = await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Create Title",
                Description: "Create Description",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 1));
            // Arrange
            var filter = new EventFilter() { Title = emptyTitle };
            var allInStore = await _eventService.GetAllEventsAsync(filter);
            var totalInStore = allInStore.TotalCount;

            // Act
            var result = await _eventService.GetAllEventsAsync(filter);

            // Assert
            Assert.Equal(totalInStore, result.TotalCount);
            Assert.Equal(totalInStore, result.Items.Count());
        }

        [Fact]
        public async Task GetAllEventsAsync_WithValidFilters_ShouldReturnAllEvents()
        {
            // Arrange
            var date1 = DateTime.UtcNow.AddDays(1);
            var date2 = DateTime.UtcNow.AddDays(2);

            await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Conference 2026",
                Description: "",
                StartAt: date1,
                EndAt: date1.AddHours(2),
                TotalSeats: 10));

            await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Conference 2027",
                Description: "",
                StartAt: date2,
                EndAt: date2.AddHours(2),
                TotalSeats: 10));

            var filter = new EventFilter { Title = "Conference 2026" };

            // Act
            var result = await _eventService.GetAllEventsAsync(filter);

            // Assert
            Assert.Single(result.Items);
        }
        #endregion

        #region GetAllEventsAsync DateBoundary Tests
        [Theory]
        [MemberData(nameof(GetDateBoundaryData))]
        public async Task GetAllEventsAsync_DateBoundaries_ShouldFilterCorrectly(
            Func<DateTime, DateTime?> startFactory,
            Func<DateTime, DateTime?> endFactory,
            int expectedCount,
            string description)
        {
            // Arrange
            var baseDate = DateTime.UtcNow.Date.AddDays(1);

            // Создаем события относительно baseDate
            await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Event 1", Description: "D",
                StartAt: baseDate.AddHours(13), EndAt: baseDate.AddHours(18), TotalSeats: 10));

            await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Event 2", Description: "D",
                StartAt: baseDate.AddHours(13), EndAt: baseDate.AddHours(18), TotalSeats: 10));

            await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Event 3", Description: "D",
                StartAt: baseDate.AddDays(1).AddHours(13), EndAt: baseDate.AddDays(1).AddHours(18), TotalSeats: 10));

            var filter = new EventFilter
            {
                From = startFactory(baseDate),
                To = endFactory(baseDate)
            };

            // Act
            var result = await _eventService.GetAllEventsAsync(filter);

            // Assert
            Assert.True(expectedCount == result.TotalCount,
                $"Ошибка в сценарии: {description}. Ожидали {expectedCount}, получили {result.TotalCount}");

            if (expectedCount > 0)
            {
                Assert.Equal(expectedCount, result.Items.Count());
            }
            else
            {
                Assert.Empty(result.Items);
            }
        }

        [Fact]
        public async Task GetAllEventsAsync_DateBoundaries_ShouldReturnOnlyEventsWithinWindow()
        {
            // Arrange
            var windowStart = DateTime.UtcNow.Date.AddDays(1);
            var windowEnd = DateTime.UtcNow.Date.AddDays(2);

            // 1. Внутри окна
            await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Inside",
                StartAt: windowStart.AddDays(1).AddHours(11),
                EndAt: windowEnd.AddDays(1).AddHours(14),
                TotalSeats: 10));

            // 2. До окна
            await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Before",
                StartAt: windowStart.AddHours(1),
                EndAt: windowEnd.AddHours(2),
                TotalSeats: 10));

            // 3. После окна
            await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "After",
                StartAt: windowStart.AddDays(2),
                EndAt: windowEnd.AddDays(2),
                TotalSeats: 10));

            var filter = new EventFilter
            {
                From = windowStart.AddDays(1).AddHours(11),
                To = windowEnd.AddDays(1).AddHours(14)
            };

            // Act
            var result = await _eventService.GetAllEventsAsync(filter);

            // Assert
            Assert.Single(result.Items);
            Assert.Equal("Inside", result.Items.First().Title);
        }

        [Fact]
        public async Task GetAllEventsAsync_DateBoundaries_FromOneSecondLate()
        {
            var baseDate = DateTime.UtcNow.Date.AddDays(1);

            await _eventService.CreateEventAsync(new EventCreateDto
            (
                Title: "Event 1",
                Description: "D",
                StartAt: baseDate.AddHours(13),
                EndAt: baseDate.AddHours(18),
                TotalSeats: 10));
            await _eventService.CreateEventAsync(new EventCreateDto
            (
                Title: "Event 2",
                Description: "D",
                StartAt: baseDate.AddHours(13),
                EndAt: baseDate.AddHours(18),
                TotalSeats: 10));
            await _eventService.CreateEventAsync(new EventCreateDto
            (
                Title: "Event 3",
                Description: "D",
                StartAt: baseDate.AddDays(1).AddHours(13),
                EndAt: baseDate.AddDays(1).AddHours(18),
                TotalSeats: 10));

            var result = await _eventService.GetAllEventsAsync(new EventFilter()
            {
                From = baseDate.AddHours(13).AddSeconds(1)
            });

            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task GetAllEventsAsync_DateBoundaries_ToOneSecondEarly()
        {
            var baseDate = DateTime.UtcNow.Date.AddDays(1);

            await _eventService.CreateEventAsync(new EventCreateDto
            (
                Title: "Event 1",
                Description: "D",
                StartAt: baseDate.AddHours(13),
                EndAt: baseDate.AddHours(18),
                TotalSeats: 10
            ));
            await _eventService.CreateEventAsync(new EventCreateDto
            (
                Title: "Event 2",
                Description: "D",
                StartAt: baseDate.AddHours(13),
                EndAt: baseDate.AddHours(18),
                TotalSeats: 10));
            await _eventService.CreateEventAsync(new EventCreateDto
            (
                Title: "Event 3",
                Description: "D",
                StartAt: baseDate.AddDays(1).AddHours(13),
                EndAt: baseDate.AddDays(1).AddHours(18),
                TotalSeats: 10));

            var result = await _eventService.GetAllEventsAsync(new EventFilter() { To = baseDate.AddHours(18).AddSeconds(-1) });

            Assert.Empty(result.Items);
        }

        public static TheoryData<Func<DateTime, DateTime?>, Func<DateTime, DateTime?>, int, string> GetDateBoundaryData() =>
            new()
            {
                // 1. Идеальное совпадение границ (13:00 - 18:00)
                {
                    baseDate => baseDate.AddHours(13),
                    baseDate => baseDate.AddHours(18),
                    2,
                    "Должны найти 2 события, которые точно вписались в границы"
                },

                // 2. Только "From" (С 13:00)
                {
                    baseDate => baseDate.AddHours(13),
                    baseDate => null,
                    3,
                    "Все события начинаются не раньше 13:00"
                },

                // 3. Только "To" (До 18:00)
                {
                    baseDate => null,
                    baseDate => baseDate.AddHours(18),
                    2,
                    "Только первые два события успевают закончиться до 18:00"
                },

                // 4. ГРАНИЦА: Опоздали на секунду (From = 13:00:01)
                {
                    baseDate => baseDate.AddHours(13).AddSeconds(1),
                    baseDate => null,
                    1, // Останется только Event 3, который будет на следующий день
                    "События 1 и 2 отсеиваются, т.к. начинаются в 13:00:00 (раньше фильтра)"
                },

                // 5. ГРАНИЦА: Не успели закончить (To = 17:59:59)
                {
                    baseDate => null,
                    baseDate => baseDate.AddHours(18).AddSeconds(-1),
                    0,
                    "Ни одно событие не закончено к этому времени"
                }
            };
        #endregion

        #region GetAllEventsAsync Pagination Tests
        [Fact]
        public async Task GetAllEventsAsync_ByPagination_ShouldReturnCorrectSlice()
        {
            // Arrange
            // Добавляем 3 события с разными датами, чтобы проверить сортировку и пропуск
            var futureDate = DateTime.UtcNow.AddDays(1);
            await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Event 1",
                Description: "D",
                StartAt: futureDate,
                EndAt: futureDate.AddDays(1),
                TotalSeats: 10));

            await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Event 2",
                Description: "D",
                StartAt: futureDate.AddDays(2),
                EndAt: futureDate.AddDays(3),
                TotalSeats: 10));

            await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Event 3",
                Description: "D",
                StartAt: futureDate.AddDays(4),
                EndAt: futureDate.AddDays(5),
                TotalSeats: 10));

            var filter = new EventFilter { Page = 2, PageSize = 1 };

            // Act
            var result = await _eventService.GetAllEventsAsync(filter);

            // Assert
            Assert.Equal(3, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(2, result.CurrentPage);
        }

        [Fact]
        public async Task GetAllEventsAsync_ByPagination_ShouldReturnPaginationEvents()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var createdEvent1 = await _eventService.CreateEventAsync(new EventCreateDto
            (
                Title: "Event 1",
                Description: "For City 1",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10)
            );
            var createdEvent2 = await _eventService.CreateEventAsync(new EventCreateDto
            (
                Title: "Event 2",
                Description: "For City 2",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10)
            );
            var createdEvent3 = await _eventService.CreateEventAsync(new EventCreateDto
            (
                Title: "Event 3",
                Description: "For City 3",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10)
            );


            int currentPage = 2;
            int pageSize = 1;


            var totalEvents = await _eventService.GetAllEventsAsync(new EventFilter());
            var expectedTotalEvents = totalEvents.TotalCount;
            var expectedReturnedEventsCount = Math.Min(pageSize, expectedTotalEvents);
            var eventFilter = new EventFilter { Page = currentPage, PageSize = pageSize };

            // Act
            var paginatedResult = await _eventService.GetAllEventsAsync(eventFilter);

            // Assert
            Assert.Equal(expectedTotalEvents, paginatedResult.TotalCount);
            Assert.Equal(expectedReturnedEventsCount, paginatedResult.Items.Count());
            Assert.Equal(currentPage, paginatedResult.CurrentPage);
        }

        [Fact]
        public async Task GetAllEventsAsync_WithDefaultPagination_ReturnsFirstPageWithDefaultPageSize()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            for (int i = 1; i <= 15; i++)
            {
                await _eventService.CreateEventAsync(new EventCreateDto(
                    Title: $"Event {i}",
                    StartAt: futureDate.AddHours(i),
                    EndAt: futureDate.AddHours(i + 1),
                    TotalSeats: 10));
            }

            // Act
            var result = await _eventService.GetAllEventsAsync(new EventFilter());

            // Assert
            Assert.Equal(15, result.TotalCount);
            Assert.Equal(1, result.CurrentPage);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(10, result.Items.Count());
            Assert.Equal(2, result.TotalPages);
        }

        [Fact]
        public async Task GetAllEventsAsync_WithCustomPageSize_ReturnsCorrectNumberOfItems()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            for (int i = 1; i <= 25; i++)
            {
                await _eventService.CreateEventAsync(new EventCreateDto
                (
                    Title: $"Event {i}",
                    StartAt: futureDate.AddHours(i),
                    EndAt: futureDate.AddHours(i + 1),
                    TotalSeats: 10)
                );
            }

            // Act
            var result = await _eventService.GetAllEventsAsync(new EventFilter() { Page = 1, PageSize = 5 });

            // Assert
            Assert.Equal(25, result.TotalCount);
            Assert.Equal(1, result.CurrentPage);
            Assert.Equal(5, result.PageSize);
            Assert.Equal(5, result.TotalPages);
        }

        [Fact]
        public async Task GetAllEventsAsync_WithLastPagePartialResults_ReturnsRemainingItems()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            for (int i = 1; i <= 23; i++)
            {
                await _eventService.CreateEventAsync(new EventCreateDto
                (
                    Title: $"Event {i}",
                    StartAt: futureDate.AddHours(i),
                    EndAt: futureDate.AddHours(i + 1),
                    TotalSeats: 10
            ));
            }

            // Act
            var result = await _eventService.GetAllEventsAsync(new EventFilter() { Page = 3, PageSize = 10 });


            // Assert
            Assert.Equal(23, result.TotalCount);
            Assert.Equal(3, result.CurrentPage);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(3, result.Items.Count());
            Assert.Equal(3, result.TotalPages);
        }

        [Fact]
        public async Task GetAllEventsAsync_WithPageBeyondTotal_ReturnsEmptyItems()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            for (int i = 1; i <= 5; i++)
            {
                await _eventService.CreateEventAsync(new EventCreateDto
                (
                    Title: $"Event {i}",
                    StartAt: futureDate.AddHours(i),
                    EndAt: futureDate.AddHours(i + 1),
                    TotalSeats: 10
                ));
            }

            // Act
            var result = await _eventService.GetAllEventsAsync(new EventFilter() { Page = 10, PageSize = 10 });

            // Assert
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(10, result.CurrentPage);
            Assert.Equal(10, result.PageSize);
            Assert.Empty(result.Items);
            Assert.Equal(1, result.TotalPages);
        }

        [Fact]
        public async Task GetAllEventsAsync_WithPaginationAndFilters_ReturnsPaginatedFilteredResults()
        {
            // Arrange
            var baseDate = DateTime.UtcNow.AddDays(1);
            for (int i = 1; i <= 30; i++)
            {
                await _eventService.CreateEventAsync(new EventCreateDto
                (
                    Title: $"Conference {i}",
                    StartAt: baseDate.AddDays(i),
                    EndAt: baseDate.AddDays(i).AddHours(2),
                    TotalSeats: 10
                ));
            }

            // Act
            var result = await _eventService.GetAllEventsAsync(new EventFilter()
            {
                Page = 2,
                PageSize = 5,
                Title = "Conference"
            });

            // Assert
            Assert.Equal(30, result.TotalCount);
            Assert.Equal(2, result.CurrentPage);
            Assert.Equal(5, result.PageSize);
            Assert.Equal(5, result.Items.Count());
            Assert.Equal(6, result.TotalPages);
        }

        [Fact]
        public async Task GetAllEventsAsync_WithSecondPage_ReturnsCorrectItems()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            for (int i = 1; i <= 25; i++)
            {
                await _eventService.CreateEventAsync(new EventCreateDto
                (
                    Title: $"Event {i}",
                    StartAt: futureDate.AddHours(i),
                    EndAt: futureDate.AddHours(i + 1),
                    TotalSeats: 10));
            }

            // Act
            var result = await _eventService.GetAllEventsAsync(new EventFilter() { Page = 2, PageSize = 10 });

            // Assert
            Assert.Equal(25, result.TotalCount);
            Assert.Equal(2, result.CurrentPage);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(10, result.Items.Count());
            Assert.Equal(3, result.TotalPages);
        }

        [Fact]
        public async Task GetAllEventsAsync_TotalPagesCalculation_IsCorrect()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            for (int i = 1; i <= 37; i++)
            {
                await _eventService.CreateEventAsync(new EventCreateDto
                (
                    Title: $"Event {i}",
                    StartAt: futureDate.AddHours(i),
                    EndAt: futureDate.AddHours(i + 1),
                    TotalSeats: 10
                ));
            }

            // Act
            var result = await _eventService.GetAllEventsAsync(new EventFilter() { PageSize = 10 });

            // Assert
            Assert.Equal(4, result.TotalPages);
        }
        #endregion

        #region GetAllEventsAsync Validation Tests
        [Theory]
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
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == expectedError);
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
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == expectedError);
        }


        #endregion

        #region GetEventByIdAsync
        [Fact]
        public async Task GetEventByIdAsync_ExistingId_ShouldRetrieveEventSuccessfully()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var createdEvent = await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Title",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10));

            // Act
            var result = await _eventService.GetEventByIdAsync(createdEvent.Id);

            // Assert
            Assert.Equal(createdEvent.Id, result.Id);
            Assert.Equal(createdEvent.Title, result.Title);
            Assert.Equal(createdEvent.Description, result.Description);
            Assert.Equal(createdEvent.StartAt, result.StartAt);
            Assert.Equal(createdEvent.EndAt, result.EndAt);
            Assert.Equal(createdEvent.TotalSeats, result.TotalSeats);
            Assert.Equal(createdEvent.AvailableSeats, result.AvailableSeats);
        }

        [Fact]
        public async Task GetEventByIdAsync_NonExisting_ShouldThrowNotFoundException()
        {
            // Arrange
            Guid expectedId = Guid.NewGuid();

            string expectedExceptionMessage = $"Событие с ID {expectedId} не найдено.";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(async () =>
                await _eventService.GetEventByIdAsync(expectedId)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
        }
        #endregion

        #region UpdateEventAsync
        [Fact]
        public async Task UpdateEventAsync_NonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            Guid expectedId = Guid.NewGuid();
            string expectedExceptionMessage = "Событие не найдено.";
            var futureDate = DateTime.UtcNow.AddDays(1);

            EventUpdateDto entityDto = new EventUpdateDto(
                Title: "Update Title",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                Description: "Update Description");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(async () =>
                await _eventService.UpdateEventAsync(expectedId, entityDto)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
        }

        [Fact]
        public async Task UpdateEventAsync_WithExistingId_ShouldModifyEventInList()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var createdEvent = await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Title",
                Description: "Desc",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10));

            var dto = new EventUpdateDto(
                Title: "Title Update",
                StartAt: futureDate.AddDays(1),
                EndAt: futureDate.AddDays(2));

            // Act
            await _eventService.UpdateEventAsync(createdEvent.Id, dto);

            // Assert
            var updated = await _eventService.GetEventByIdAsync(createdEvent.Id);
            Assert.Equal(dto.Title, updated.Title);
            Assert.Equal(dto.Description, updated.Description);
            Assert.Equal(dto.StartAt, updated.StartAt);
            Assert.Equal(dto.EndAt, updated.EndAt);
        }

        [Fact]
        public async Task UpdateEventAsync_WithEndAtBeforeStartAt_ShouldThrowValidationException()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            EventCreateDto createDto = new EventCreateDto(
                Title: "Create Title",
                Description: "Create Description",
                StartAt: futureDate,
                EndAt: futureDate.AddDays(1),
                TotalSeats: 1);
            var eventEntity = await _eventService.CreateEventAsync(createDto);

            string title = "Update Title";
            string description = "Update Description";

            EventUpdateDto updateDto = new EventUpdateDto(
                Title: title,
                Description: description,
                StartAt: futureDate,
                EndAt: futureDate.AddDays(-1));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
                await _eventService.UpdateEventAsync(eventEntity.Id, updateDto)
            );

            // Assert
            Assert.Contains("endAt", exception.ValidationResult.MemberNames);
        }

        [Fact]
        public async Task UpdateEventAsync_WithNullTitle_ThrowsValidationException()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var createdEvent = await _eventService.CreateEventAsync(new EventCreateDto
            (
                Title: "Original Event",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10));

            var updateEvent = new EventUpdateDto
            (
                Title: null,
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2));

            // Act
            var exception = await Assert.ThrowsAsync<ValidationException>(() =>
                _eventService.UpdateEventAsync(createdEvent.Id, updateEvent));

            // Assert
            Assert.Contains("title", exception.ValidationResult.MemberNames);
        }

        [Fact]
        public async Task UpdateEventAsync_WithPastStartAt_ThrowsValidationException()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var createdEvent = await _eventService.CreateEventAsync(new EventCreateDto
            (
                Title: "Original Event",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10));

            var pastDate = DateTime.UtcNow.AddDays(-1);
            var updateEvent = new EventUpdateDto(
                Title: "Updated Event",
                StartAt: pastDate,
                EndAt: pastDate.AddHours(2));

            // Act
            var exception = await Assert.ThrowsAsync<ValidationException>(() =>
                _eventService.UpdateEventAsync(createdEvent.Id, updateEvent));

            // Assert
            Assert.Contains("startAt", exception.ValidationResult.MemberNames);
        }

        [Fact]
        public async Task UpdateEventAsync_WithValidData_UpdatesEvent()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var createdEvent = await _eventService.CreateEventAsync(new EventCreateDto
            (
                Title: "Original Event",
                Description: "Original Description",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10));

            var newFutureDate = DateTime.UtcNow.AddDays(2);
            var updateEvent = new EventUpdateDto(
                Title: "Updated Event",
                Description: "Updated Description",
                StartAt: newFutureDate,
                EndAt: newFutureDate.AddHours(3));

            // Act
            var result = await _eventService.UpdateEventAsync(createdEvent.Id, updateEvent);

            // Assert
            Assert.Equal(createdEvent.Id, result.Id);
            Assert.Equal("Updated Event", result.Title);
            Assert.Equal("Updated Description", result.Description);
            Assert.Equal(newFutureDate, result.StartAt);
            Assert.Equal(newFutureDate.AddHours(3), result.EndAt);
        }
        #endregion
    }
}