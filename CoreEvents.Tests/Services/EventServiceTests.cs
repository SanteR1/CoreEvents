using CoreEvents.Data.Repositories;
using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;
using CoreEvents.Services;

using Moq;

namespace CoreEvents.Tests.Services
{
    public class EventServiceTests
    {
        private readonly Mock<IRepository<EventEntity>> _mockRepository;
        private readonly EventService _eventService;
        private readonly List<EventEntity> _eventsList;
        public EventServiceTests()
        {
            _eventsList = new List<EventEntity>
            {
                new EventEntity { Id = new Guid("9ab82324-d774-42fd-a2a8-58dcfe22a174"),
                    Title = "Abc",
                    Description = "Test 1",
                    StartAt = new DateTime(2026, 01, 01, 13, 00, 00),
                    EndAt = new DateTime(2026, 01, 01, 18, 00, 00)},
                new EventEntity { Id = new Guid("8d1499d8-02fa-4e83-a2eb-6f8dbf13014c"),
                    Title = "Cdf",
                    Description = "Test 2",
                    StartAt = new DateTime(2026, 01, 01, 13, 00, 00),
                    EndAt = new DateTime(2026, 01, 01, 18, 00, 00)},
                new EventEntity { Id = new Guid("bbf1cb27-02af-4253-927b-2aece4724434"),
                    Title = "Nrh",
                    Description = "Test 3",
                    StartAt = new DateTime(2026, 01, 02, 13, 00, 00),
                    EndAt = new DateTime(2026, 01, 02, 18, 00, 00)}
            };
            _mockRepository = new Mock<IRepository<EventEntity>>();
            _mockRepository
                .Setup(repo => repo
                .GetAll())
                .Returns(_eventsList);
            _mockRepository
                .Setup(repo => repo
                .GetById(It.IsAny<Guid>()))
                .Returns((Guid id) => _eventsList.FirstOrDefault(x => x.Id == id));
            _mockRepository
                .Setup(repo => repo
                .Delete(It.IsAny<Guid>()))
                .Callback((Guid id) => _eventsList.RemoveAll(x => x.Id == id));
            _eventService = new EventService(_mockRepository.Object);
        }

        [Fact]
        public void CreateEvent_WithValidData_ShouldReturnCreatedEvent()
        {
            // Arrange
            string expectedTitle = "Event Title";
            string expectedDescription = "Event Description";
            DateTime expectedDateTimeStartAt = new DateTime(2026, 01, 01, 13, 00, 00);
            DateTime expectedDateTimeEndAt = new DateTime(2026, 01, 01, 18, 00, 00);
            EventCreateDto expectedCreateDto = new EventCreateDto(expectedTitle, expectedDescription, expectedDateTimeStartAt, expectedDateTimeEndAt);

            // Act
            var result = _eventService.CreateEvent(expectedCreateDto);

            // Assert
            Assert.Equal(expectedTitle, result.Title);
            Assert.Equal(expectedDescription, result.Description);
            Assert.Equal(expectedDateTimeStartAt, result.StartAt);
            Assert.Equal(expectedDateTimeEndAt, result.EndAt);
        }

        [Fact]
        public void GetEvents_WithValidFilters_ShouldReturnAllEvents()
        {
            // Arrange
            string optionalTitle = "C";
            DateTime optionalDateTimeStartAt = new DateTime(2026, 01, 01, 13, 00, 00);
            DateTime optionalDateTimeEndAt = new DateTime(2026, 01, 01, 18, 00, 00);
            EventFilter eventFilter = new EventFilter(optionalTitle, optionalDateTimeStartAt, optionalDateTimeEndAt);
            int expectedPage = 1;
            int expectedPageSize = 10;
            int expectedEvents = 2;
            int expectedTotalEvents = 2;

            // Act
            var result = _eventService.GetEvents(eventFilter);

            // Assert
            Assert.Equal(expectedPage, result.CurrentPage);
            Assert.Equal(expectedPageSize, result.PageSize);
            Assert.Equal(expectedEvents, result.Events.Count());
            Assert.Equal(expectedTotalEvents, result.TotalEvents);
        }

        [Fact]
        public void GetEventById_ExistingId_ShouldRetrieveEventSuccessfully()
        {
            // Arrange
            Guid expectedId = new Guid("9ab82324-d774-42fd-a2a8-58dcfe22a174");
            string expectedTitle = "Abc";
            string expectedDescription = "Test 1";
            DateTime expectedDateTimeStartAt = new DateTime(2026, 01, 01, 13, 00, 00);
            DateTime expectedDateTimeEndAt = new DateTime(2026, 01, 01, 18, 00, 00);

            // Act
            var result = _eventService.GetEventById(expectedId);

            // Assert
            Assert.Equal(expectedId, result.Id);
            Assert.Equal(expectedTitle, result.Title);
            Assert.Equal(expectedDescription, result.Description);
            Assert.Equal(expectedDateTimeStartAt, result.StartAt);
            Assert.Equal(expectedDateTimeEndAt, result.EndAt);
        }

        [Fact]
        public void UpdateEvent_WithExistingId_ShouldModifyEventInList()
        {
            // Arrange
            Guid expectedId = new Guid("9ab82324-d774-42fd-a2a8-58dcfe22a174");
            string expectedTitle = "Updated Title";
            string expectedDescription = "Updated Description";
            DateTime expectedDateTimeStartAt = new DateTime(2025, 01, 01, 13, 00, 00);
            DateTime expectedDateTimeEndAt = new DateTime(2025, 01, 01, 18, 00, 00);
            EventCreateDto eventCreateDto = new EventCreateDto(expectedTitle, expectedDescription, expectedDateTimeStartAt, expectedDateTimeEndAt);

            // Act
            _eventService.UpdateEvent(expectedId, eventCreateDto);

            // Assert
            var updatedEvent = _eventsList.First(x => x.Id == expectedId);
            Assert.Equal(expectedTitle, updatedEvent.Title);
            Assert.Equal(expectedDescription, updatedEvent.Description);
            Assert.Equal(expectedDateTimeStartAt, updatedEvent.StartAt);
            Assert.Equal(expectedDateTimeEndAt, updatedEvent.EndAt);

            _mockRepository.Verify(r => r.Update(updatedEvent), Times.Once);
        }

        [Fact]
        public void DeleteEvent_WithExistingId_ShouldRemoveEventFromList()
        {
            // Arrange
            int initialCount = _eventsList.Count;
            Guid expectedId = new Guid("9ab82324-d774-42fd-a2a8-58dcfe22a174");

            // Act
            _eventService.DeleteEvent(expectedId);

            // Assert
            Assert.DoesNotContain(_eventsList, x => x.Id == expectedId);
            Assert.Equal(initialCount - 1, _eventsList.Count);
            _mockRepository.Verify(r => r.Delete(expectedId), Times.Once);
        }

        [Fact]
        public void GetEvents_ByTitleFilter_ShouldReturnFilteredEvents()
        {
            // Arrange
            string filterTitle = "df";
            int expectedCount = 1;
            EventFilter eventFilter = new EventFilter(filterTitle);
            string expectedTitle = "Cdf";
            string expectedDescription = "Test 2";

            // Act
            var result = _eventService.GetEvents(eventFilter);

            // Assert
            Assert.Equal(expectedCount, result.TotalEvents);

            var items = result.Events.ToList();
            Assert.Single(items);

            var actualEvent = items.First();
            Assert.Equal(expectedTitle, actualEvent.Title);
            Assert.Equal(expectedDescription, actualEvent.Description);
        }

        [Fact]
        public void GetEvents_ByDateFilter_ShouldReturnFilteredEvents()
        {
            // Arrange
            int expectedCount = 1;
            DateTime filterStartAt = new DateTime(2026, 01, 02, 13, 00, 00);
            DateTime filterEndAt = new DateTime(2026, 01, 02, 18, 00, 00);
            EventFilter eventFilter = new EventFilter(From: filterStartAt, To: filterEndAt);
            string expectedTitle = "Nrh";
            string expectedDescription = "Test 3";
            DateTime expectedStartAt = new DateTime(2026, 01, 02, 13, 00, 00);
            DateTime expectedEndAt = new DateTime(2026, 01, 02, 18, 00, 00);

            // Act
            var result = _eventService.GetEvents(eventFilter);

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
        public void GetEvents_ByPagination_ShouldReturnPaginationEvents()
        {
            // Arrange
            int currentPage = 2;
            int pageSize = 1;
            var expectedTotalEvents = _eventsList.Count;
            var expectedReturnedEventsCount = Math.Min(pageSize, expectedTotalEvents);
            var eventFilter = new EventFilter { Page = currentPage, PageSize = pageSize };
            // Act
            var paginatedResult = _eventService.GetEvents(eventFilter);

            // Assert
            Assert.Equal(expectedTotalEvents, paginatedResult.TotalEvents);
            Assert.Equal(expectedReturnedEventsCount, paginatedResult.Events.Count());
            Assert.Equal(currentPage, paginatedResult.CurrentPage);
        }

        [Fact]
        public void GetEvents_ByTitleAndDateAndPageFilter_ShouldReturnFilteredEvents()
        {
            // Arrange
            string filterTitle = "c";
            int expectedCount = 2;
            DateTime filterStartAt = new DateTime(2026, 01, 01, 13, 00, 00);
            DateTime filterEndAt = new DateTime(2026, 01, 01, 18, 00, 00);
            int currentPage = 1;
            int pageSize = 1;
            EventFilter eventFilter = new EventFilter(Title: filterTitle, Page: currentPage, PageSize: pageSize, From: filterStartAt, To: filterEndAt);
            string expectedTitle = "c";
            DateTime expectedStartAt = new DateTime(2026, 01, 01, 13, 00, 00);
            DateTime expectedEndAt = new DateTime(2026, 01, 01, 18, 00, 00);

            // Act
            var result = _eventService.GetEvents(eventFilter);

            // Assert
            Assert.Equal(expectedCount, result.TotalEvents);

            var items = result.Events.ToList();
            Assert.Single(items);

            var actualEvent = items.First();
            Assert.Contains(expectedTitle, actualEvent.Title);

            Assert.Equal(expectedStartAt, actualEvent.StartAt);
            Assert.Equal(expectedEndAt, actualEvent.EndAt);
        }

        [Fact]
        public void GetEventById_NonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            Guid expectedId = Guid.NewGuid();
            string expectedExceptionMessage = $"Событие с ID {expectedId} не найдено.";

            // Act & Assert
            var exception = Assert.Throws<KeyNotFoundException>(() =>
                _eventService.GetEventById(expectedId)
            );

            //Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            _mockRepository.Verify(r => r.GetById(expectedId), Times.Once);
        }

        [Fact]
        public void UpdateEvent_NonExistingId_ShouldReturnNotFound()
        {
            // Arrange
            Guid expectedId = Guid.NewGuid();
            string expectedExceptionMessage = $"Событие не найдено.";
            string title = "Update Title";
            string description = "Update Description";
            var expectedStartAt = new DateTime(2026, 01, 02, 13, 00, 00);
            var expectedEndAt = new DateTime(2026, 01, 01, 18, 00, 00);
            EventCreateDto entityDto = new EventCreateDto(Title: title, Description: description, StartAt: expectedStartAt, EndAt: expectedEndAt);

            // Act & Assert
            var exception = Assert.Throws<KeyNotFoundException>(() =>
                _eventService.UpdateEvent(expectedId,entityDto)
            );

            //Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            _mockRepository.Verify(r => r.Update(It.IsAny<EventEntity>()), Times.Never);
        }

        [Fact]
        public void UpdateEvent_WithNonValidDate_ShouldReturnArgumentException()
        {
            // Arrange
            Guid expectedId = new Guid("9ab82324-d774-42fd-a2a8-58dcfe22a174");
            string title = "Update Title";
            string description = "Update Description";
            var expectedStartAt = new DateTime(2026, 01, 02, 13, 00, 00);
            var expectedEndAt = new DateTime(2026, 01, 01, 18, 00, 00);
            string expectedExceptionMessage = "Дата окончания должна быть позже даты начала.";
            EventCreateDto entityDto = new EventCreateDto(Title: title, Description: description, StartAt: expectedStartAt, EndAt: expectedEndAt);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                _eventService.UpdateEvent(expectedId, entityDto)
            );

            //Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            _mockRepository.Verify(r => r.Update(It.IsAny<EventEntity>()), Times.Never);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetEvents_EmptyFilter_ShouldReturnAllEvents(string? emptyTitle)
        {
            // Arrange
            var filter = new EventFilter(emptyTitle);
            var totalInStore = _eventsList.Count;

            // Act
            var result = _eventService.GetEvents(filter);

            // Assert
            Assert.Equal(totalInStore, result.TotalEvents);
            Assert.Equal(totalInStore, result.Events.Count());
        }

        [Theory]
        [MemberData(nameof(GetDateBoundaryData))]
        public void GetEvents_DateBoundaries_ShouldFilterCorrectly(
            DateTime? start,
            DateTime? end,
            int expectedCount,
            string description)
        {
            // Arrange
            var filter = new EventFilter(From: start, To: end);

            // Act
            var result = _eventService.GetEvents(filter);

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