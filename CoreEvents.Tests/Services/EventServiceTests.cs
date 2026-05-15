using System.ComponentModel.DataAnnotations;
using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;
using CoreEvents.Services.Implementations;
using CoreEvents.Tests.Infrastructure;
using Moq;

namespace CoreEvents.Tests.Services
{
    public class EventServiceTests
    {
        private readonly TestContext _ctx;
        private readonly EventService _eventService;

        public EventServiceTests()
        {
            _ctx = new TestContext();
            _ctx.SetupMocks();
            _eventService = new EventService(_ctx.EventRepo.Object);
        }

        [Fact]
        public async Task CreateEvent_WithValidData_ShouldReturnCreatedEvent()
        {
            // Arrange
            var dto = new EventCreateDto("Title", "Desc", new DateTime(2026, 1, 1, 13, 0, 0), new DateTime(2026, 1, 1, 18, 0, 0), 5);

            // Act
            var result = await _eventService.CreateEvent(dto);

            // Assert
            Assert.Equal(dto.Title, result.Title);
            Assert.Equal(dto.Description, result.Description);
            Assert.Equal(dto.StartAt, result.StartAt);
            Assert.Equal(dto.EndAt, result.EndAt);
            Assert.Equal(dto.TotalSeats, result.TotalSeats);
        }

        [Fact]
        public async Task GetEvents_WithValidFilters_ShouldReturnAllEvents()
        {
            // Arrange
            _ctx.AddEvent("Cdf");
            _ctx.AddEvent("Ghj");

            var filter = new EventFilter { Title = "Cdf" };

            // Act
            var result = await _eventService.GetEvents(filter);

            // Assert
            Assert.Single(result.Events);
        }

        [Fact]
        public async Task GetEventById_ExistingId_ShouldRetrieveEventSuccessfully()
        {
            // Arrange
            var eventEntity = _ctx.AddEvent("Test Event");

            // Act
            var result = await _eventService.GetEventById(eventEntity.Id);

            // Assert
            Assert.Equal(eventEntity.Id, result.Id);
            Assert.Equal(eventEntity.Title, result.Title);
            Assert.Equal(eventEntity.Description, result.Description);
            Assert.Equal(eventEntity.StartAt, result.StartAt);
            Assert.Equal(eventEntity.EndAt, result.EndAt);
            Assert.Equal(eventEntity.TotalSeats, result.TotalSeats);
            Assert.Equal(eventEntity.AvailableSeats, result.AvailableSeats);
            _ctx.EventRepo.Verify(r => r.GetById(It.IsAny<Guid>()), Times.Once);
        }

        [Fact]
        public async Task UpdateEvent_WithExistingId_ShouldModifyEventInList()
        {
            // Arrange
            var eventEntity = _ctx.AddEvent("Test Event");

            var dto = new EventCreateDto("New Title", "New Desc", eventEntity.StartAt, eventEntity.EndAt, 1);

            // Act
            await _eventService.UpdateEvent(eventEntity.Id, dto);

            // Assert
            var updated = _ctx.Events.First(x => x.Id == eventEntity.Id);
            Assert.Equal(dto.Title, updated.Title);
            Assert.Equal(dto.Description, updated.Description);
            Assert.Equal(dto.StartAt, updated.StartAt);
            Assert.Equal(dto.EndAt, updated.EndAt);
            _ctx.EventRepo.Verify(r => r.Update(It.IsAny<EventEntity>()), Times.Once);

        }

        [Fact]
        public async Task DeleteEvent_WithExistingId_ShouldRemoveEventFromList()
        {
            // Arrange
            var eventEntity = _ctx.AddEvent("Test Event");
            int initialCount = _ctx.Events.Count;

            // Act
            await _eventService.DeleteEvent(eventEntity.Id);

            // Assert
            Assert.DoesNotContain(_ctx.Events, x => x.Id == eventEntity.Id);
            Assert.Equal(initialCount - 1, _ctx.Events.Count);
            _ctx.EventRepo.Verify(r => r.Delete(eventEntity.Id), Times.Once);
        }

        [Fact]
        public async Task GetEvents_ByTitleFilter_ShouldReturnFilteredEvents()
        {
            // Arrange
            var eventEntity1 = _ctx.AddEvent("Main Events 1");
            _ctx.AddEvent("Main Events 2");
            EventFilter eventFilter = new EventFilter() { Title = eventEntity1.Title };

            // Act
            var result = await _eventService.GetEvents(eventFilter);

            // Assert
            Assert.Equal(1, result.TotalEvents);

            var items = result.Events.ToList();
            Assert.Single(items);

            var actualEvent = items.First();
            Assert.Equal(eventFilter.Title, actualEvent.Title);
        }

        [Fact]
        public async Task GetEvents_ByDateFilter_ShouldReturnFilteredEvents()
        {
            // Arrange
            int expectedCount = 1;
            DateTime expectedAndFilterStartAt = new DateTime(2026, 01, 02, 13, 00, 00);
            DateTime expectedAndFilterEndAt = new DateTime(2026, 01, 02, 18, 00, 00);
            EventFilter eventFilter = new EventFilter() { From = expectedAndFilterStartAt, To = expectedAndFilterEndAt };
            string expectedTitle = "Event 2026 01 02";
            string expectedDescription = "For City";
            _ctx.AddEvent(expectedTitle, expectedDescription, expectedAndFilterStartAt, expectedAndFilterEndAt, 1);

            // Act
            var result = await _eventService.GetEvents(eventFilter);

            // Assert
            Assert.Equal(expectedCount, result.TotalEvents);

            var items = result.Events.ToList();
            Assert.Single(items);

            var actualEvent = items.First();
            Assert.Equal(expectedTitle, actualEvent.Title);
            Assert.Equal(expectedDescription, actualEvent.Description);
            Assert.Equal(expectedAndFilterStartAt, actualEvent.StartAt);
            Assert.Equal(expectedAndFilterEndAt, actualEvent.EndAt);
        }

        [Fact]
        public async Task GetEvents_FromAndToDateOnly_ShouldIncludeAllEventsForThatDay()
        {
            // Arrange
            int expectedCount = 1;
            DateTime filterStartAt = new DateTime(2026, 01, 02);
            DateTime filterEndAt = new DateTime(2026, 01, 02);
            EventFilter eventFilter = new EventFilter() { From = filterStartAt, To = filterEndAt };
            string expectedTitle = "Nrh";
            string expectedDescription = "Test 3";
            DateTime expectedStartAt = new DateTime(2026, 01, 02, 13, 00, 00);
            DateTime expectedEndAt = new DateTime(2026, 01, 02, 18, 00, 00);
            _ctx.AddEvent(expectedTitle, expectedDescription, expectedStartAt, expectedEndAt, 1);
            _ctx.AddEvent(expectedTitle, expectedDescription, expectedStartAt.AddDays(1), expectedEndAt.AddDays(1), 1);

            // Act
            var result = await _eventService.GetEvents(eventFilter);

            // Assert
            Assert.Equal(expectedCount, result.TotalEvents);

            var items = result.Events.ToList();
            Assert.Single(items);

            var actualEvent = items.First();
            Assert.Equal(expectedTitle, actualEvent.Title);
            Assert.Equal(expectedDescription, actualEvent.Description);

            Assert.Equal(expectedStartAt, actualEvent.StartAt);
            Assert.Equal(expectedEndAt, actualEvent.EndAt);
        }

        [Fact]
        public async Task GetEvents_ByPagination_ShouldReturnPaginationEvents()
        {
            // Arrange
            int currentPage = 2;
            int pageSize = 1;
            _ctx.AddEvent("Main Events 1");
            _ctx.AddEvent("Main Events 2");
            _ctx.AddEvent("Main Events 3");

            var expectedTotalEvents = _ctx.Events.Count;
            var expectedReturnedEventsCount = Math.Min(pageSize, expectedTotalEvents);
            var eventFilter = new EventFilter { Page = currentPage, PageSize = pageSize };

            // Act
            var paginatedResult = await _eventService.GetEvents(eventFilter);

            // Assert
            Assert.Equal(expectedTotalEvents, paginatedResult.TotalEvents);
            Assert.Equal(expectedReturnedEventsCount, paginatedResult.Events.Count());
            Assert.Equal(currentPage, paginatedResult.CurrentPage);
        }

        [Fact]
        public async Task GetEvents_ByTitleFilter_ShouldReturnMatching()
        {
            // Arrange
            _ctx.AddEvent("Programming 1C");
            _ctx.AddEvent("Other");
            _ctx.AddEvent("Programming Python");
            var filter = new EventFilter { Title = "Programming" };

            // Act
            var result = await _eventService.GetEvents(filter);

            // Assert
            Assert.Equal(2, result.TotalEvents);
            Assert.All(result.Events, e => Assert.Contains("Programming", e.Title));
        }
        
        [Fact]
        public async Task GetEvents_FilterByDateRange_ShouldReturnOnlyEventsWithinWindow()
        {
            // Arrange
            var windowStart = new DateTime(2026, 01, 01, 10, 00, 00);
            var windowEnd = new DateTime(2026, 01, 01, 20, 00, 00);

            // 1. Внутри окна
            _ctx.AddEvent("Inside", "Desc", new DateTime(2026, 01, 01, 12, 00, 00),
                new DateTime(2026, 01, 01, 14, 00, 00), 1);
            // 2. До окна
            _ctx.AddEvent("Before", "Desc", new DateTime(2026, 01, 01, 08, 00, 00),
                new DateTime(2026, 01, 01, 09, 00, 00), 1);
            // 3. После окна
            _ctx.AddEvent("After", "Desc", new DateTime(2026, 01, 01, 22, 00, 00),
                new DateTime(2026, 01, 01, 23, 00, 00), 1);

            var filter = new EventFilter { From = windowStart, To = windowEnd };

            // Act
            var result = await _eventService.GetEvents(filter);

            // Assert
            Assert.Single(result.Events);
            Assert.Equal("Inside", result.Events.First().Title);
        }
        
        [Fact]
        public async Task GetEvents_Pagination_ShouldReturnCorrectSlice()
        {
            // Arrange
            // Добавляем 3 события с разными датами, чтобы проверить сортировку и пропуск
            _ctx.AddEvent("Event 1", "D", new DateTime(2026, 01, 01), new DateTime(2026, 01, 02), 1);
            _ctx.AddEvent("Event 2", "D", new DateTime(2026, 01, 03), new DateTime(2026, 01, 04), 1);
            _ctx.AddEvent("Event 3", "D", new DateTime(2026, 01, 05), new DateTime(2026, 01, 06), 1);

            var filter = new EventFilter { Page = 2, PageSize = 1 };

            // Act
            var result = await _eventService.GetEvents(filter);

            // Assert
            Assert.Equal(3, result.TotalEvents);
            Assert.Single(result.Events);
            Assert.Equal(2, result.CurrentPage);
        }

        [Fact]
        public async Task GetEvents_CombinedFilters_ShouldReturnCorrectSubset()
        {
            // Arrange
            var targetDate = new DateTime(2026, 01, 01, 13, 00, 00);
            var targetEnd = new DateTime(2026, 01, 01, 18, 00, 00);
            var targetTitle = "Target";

            // 1. Подходит под всё
            _ctx.AddEvent(targetTitle, "D", targetDate, targetEnd, 1);
            // 2. Подходит по заголовку, но не по дате
            _ctx.AddEvent(targetTitle, "D", targetDate.AddDays(1), targetEnd.AddDays(1), 1);
            // 3. Подходит по дате, но не по заголовку
            _ctx.AddEvent("Other", "D", targetDate, targetEnd, 1);

            var filter = new EventFilter
            {
                Title = targetTitle,
                From = targetDate,
                To = targetEnd,
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = await _eventService.GetEvents(filter);

            // Assert
            Assert.Single(result.Events);
            Assert.Equal(targetTitle, result.Events.First().Title);
            Assert.Equal(targetDate, result.Events.First().StartAt);
        }

        [Fact]
        public async Task GetEventById_NonExisting_ShouldThrowKeyNotFoundException()
        {                 
            // Arrange
            Guid expectedId = Guid.NewGuid();
            _ctx.AddEvent("Main Events 1");
            string expectedExceptionMessage = $"Событие с ID {expectedId} не найдено.";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _eventService.GetEventById(expectedId)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            _ctx.EventRepo.Verify(r => r.GetById(expectedId), Times.Once);
        }

        [Fact]
        public async Task UpdateEvent_NonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            _ctx.AddEvent("Main Events 1");
            Guid expectedId = Guid.NewGuid();
            string expectedExceptionMessage = "Событие не найдено.";
            string title = "Update Title";
            string description = "Update Description";
            var expectedStartAt = new DateTime(2026, 01, 02, 13, 00, 00);
            var expectedEndAt = new DateTime(2026, 01, 01, 18, 00, 00);
            EventCreateDto entityDto = new EventCreateDto(Title: title, Description: description, StartAt: expectedStartAt, EndAt: expectedEndAt, 1);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _eventService.UpdateEvent(expectedId, entityDto)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            _ctx.EventRepo.Verify(r => r.Update(It.IsAny<EventEntity>()), Times.Never);
        }

        [Fact]
        public async Task CreateEvent_WithNonValidDate_ShouldReturnArgumentException()
        {
            // Arrange
            string title = "Create Title";
            string description = "Create Description";
            var expectedStartAt = new DateTime(2026, 01, 02, 13, 00, 00);
            var expectedEndAt = new DateTime(2026, 01, 01, 18, 00, 00);
            string expectedExceptionMessage = "Дата окончания не может быть раньше даты начала.";
            EventCreateDto entityDto = new EventCreateDto(Title: title, Description: description, StartAt: expectedStartAt, EndAt: expectedEndAt, 1);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _eventService.CreateEvent(entityDto)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            _ctx.EventRepo.Verify(r => r.Add(It.IsAny<EventEntity>()), Times.Never);
        }

        [Fact]
        public async Task UpdateEvent_WithInvalidDate_ShouldThrowArgumentException()
        {
            // Arrange
            var  eventEntity = _ctx.AddEvent("Main Events 1");
            string title = "Update Title";
            string description = "Update Description";
            var expectedStartAt = new DateTime(2026, 01, 02, 13, 00, 00);
            var expectedEndAt = new DateTime(2026, 01, 01, 18, 00, 00);
            string expectedExceptionMessage = "Дата окончания должна быть позже даты начала.";
            EventCreateDto entityDto = new EventCreateDto(Title: title, Description: description, StartAt: expectedStartAt, EndAt: expectedEndAt, 1);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _eventService.UpdateEvent(eventEntity.Id, entityDto)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            _ctx.EventRepo.Verify(r => r.Update(It.IsAny<EventEntity>()), Times.Never);
        }

        [Theory]
        [InlineData(0, "Значение должно быть в диапазоне от 1 до 100000")]
        [InlineData(100001, "Значение должно быть в диапазоне от 1 до 100000")]
        public void GetEvents_InvalidPagePagination_ShouldReturnValidationError(int page, string expectedError)
        {
            // Arrange
            var filter = new EventFilter { Page = page};

            // Act
            var context = new ValidationContext(filter);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(filter, context, results, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage == expectedError);
            _ctx.EventRepo.Verify(r => r.GetAll(), Times.Never);
        }

        [Theory]
        [InlineData(0, "Значение должно быть в диапазоне от 1 до 100")]
        [InlineData(-1, "Значение должно быть в диапазоне от 1 до 100")]
        [InlineData(101, "Значение должно быть в диапазоне от 1 до 100")]
        public void GetEvents_InvalidPageSizePagination_ShouldReturnValidationError(int pageSize, string expectedError)
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
            _ctx.EventRepo.Verify(r => r.GetAll(), Times.Never);
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetEvents_EmptyFilter_ShouldReturnAllEvents(string? emptyTitle)
        {
            _ctx.AddEvent("Events 1");
            // Arrange
            var filter = new EventFilter() { Title = emptyTitle };
            var totalInStore = _ctx.Events.Count;

            // Act
            var result = await _eventService.GetEvents(filter);

            // Assert
            Assert.Equal(totalInStore, result.TotalEvents);
            Assert.Equal(totalInStore, result.Events.Count());
        }

        [Theory]
        [MemberData(nameof(GetDateBoundaryData))]
        public async Task GetEvents_DateBoundaries_ShouldFilterCorrectly(DateTime? start, DateTime? end, int expectedCount, string description)
        {
            _ctx.AddEvent("Event 1", "Des", new DateTime(2026, 01, 01, 13, 00, 00),
                new DateTime(2026, 01, 01, 18, 00, 00),1);

            _ctx.AddEvent("Event 2", "Des", new DateTime(2026, 01, 01, 13, 00, 00),
                new DateTime(2026, 01, 01, 18, 00, 00), 1);

            _ctx.AddEvent("Event 3", "Des", new DateTime(2026, 01, 02, 13, 00, 00),
                new DateTime(2026, 01, 02, 18, 00, 00), 1);

            // Arrange
            var filter = new EventFilter() { From = start, To = end };

            // Act
            var result = await _eventService.GetEvents(filter);

            // Assert
            Assert.True(expectedCount == result.TotalEvents,
                $"Ошибка в сценарии: {description}. Ожидали {expectedCount}, получили {result.TotalEvents}");

            if (expectedCount > 0)
            {
                Assert.Equal(expectedCount, result.Events.Count());
            }
            else
            {
                Assert.Empty(result.Events);
            }
        }

        public static TheoryData<DateTime?, DateTime?, int, string> GetDateBoundaryData() =>
            new()
            {
                // 1. Идеальное совпадение границ (01.01 13:00 - 18:00)
                {
                    new DateTime(2026, 01, 01, 13, 00, 00),
                    new DateTime(2026, 01, 01, 18, 00, 00),
                    2,
                    "Должны найти 2 события, которые точно вписались в границы"
                },

                // 2. Только "From" (С 13:00 01.01)
                {
                    new DateTime(2026, 01, 01, 13, 00, 00),
                    null,
                    3,
                    "Все события начинаются не раньше 13:00 первого числа"
                },

                // 3. Только "To" (До 18:00 01.01)
                {
                    null,
                    new DateTime(2026, 01, 01, 18, 00, 00),
                    2,
                    "Только первые два события успевают закончиться до 18:00"
                },

                // 4. ГРАНИЦА: Опоздали на секунду (From = 13:00:01)
                {
                    new DateTime(2026, 01, 01, 13, 00, 01),
                    null,
                    1,
                    "События 1 и 2 отсеиваются, т.к. начинаются в 13:00:00 (раньше фильтра)"
                },

                // 5. ГРАНИЦА: Не успели закончить (To = 17:59:59)
                {
                    null,
                    new DateTime(2026, 01, 01, 17, 59, 59),
                    0,
                    "Ни одно событие не закончено к этому времени"
                }
            };
    }
}