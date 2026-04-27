using System.Collections.Concurrent;
using CoreEvents.Data.Queues;
using CoreEvents.Data.Repositories;
using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;
using CoreEvents.Services.Implementations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CoreEvents.Tests.Services
{
    public class BookingServiceTests
    {

        private readonly Mock<IRepository<Booking>> _mockRepository;
        private readonly Mock<IRepository<EventEntity>> _eventRepoMock;
        private readonly Mock<IQueueSource<Guid>> _mockBookingQueue;
        private readonly Queue<Guid> _testQueue = new();
        private readonly BookingService _bookingService;
        private readonly EventService _eventService;
        private readonly List<EventEntity> _eventsList;
        private readonly ConcurrentDictionary<Guid, Booking> _dictionary = new();

        public BookingServiceTests()
        {
            _eventsList = new List<EventEntity>
            {
                new EventEntity {
                    Id = new Guid("9ab82324-d774-42fd-a2a8-58dcfe22a174"),
                    Title = "Abc",
                    Description = "Test 1",
                    StartAt = new DateTime(2026, 01, 01, 13, 00, 00),
                    EndAt = new DateTime(2026, 01, 01, 18, 00, 00)},
                new EventEntity {
                    Id = new Guid("8d1499d8-02fa-4e83-a2eb-6f8dbf13014c"),
                    Title = "Cdf",
                    Description = "Test 2",
                    StartAt = new DateTime(2026, 01, 01, 13, 00, 00),
                    EndAt = new DateTime(2026, 01, 01, 18, 00, 00)},
                new EventEntity {
                    Id = new Guid("bbf1cb27-02af-4253-927b-2aece4724434"),
                    Title = "Nrh",
                    Description = "Test 3",
                    StartAt = new DateTime(2026, 01, 02, 13, 00, 00),
                    EndAt = new DateTime(2026, 01, 02, 18, 00, 00)}
            };

            _dictionary.TryAdd(
                new Guid("5e68cc94-9dd9-44ae-8ea4-2652f55589ee"),
                new Booking()
                {
                    Id = new Guid("5e68cc94-9dd9-44ae-8ea4-2652f55589ee"),
                    EventId = new Guid("9ab82324-d774-42fd-a2a8-58dcfe22a174"),
                    Status = BookingStatus.Pending,
                    CreatedAt = new DateTime(2026, 01, 01, 08, 00, 00)
                }
            );
            _dictionary.TryAdd(
                new Guid("96263b7a-f285-44fc-9964-5d3ba6155f0d"),
                new Booking()
                {
                    Id = new Guid("96263b7a-f285-44fc-9964-5d3ba6155f0d"),
                    EventId = new Guid("9ab82324-d774-42fd-a2a8-58dcfe22a174"),
                    Status = BookingStatus.Pending,
                    CreatedAt = new DateTime(2026, 01, 01, 08, 30, 00)
                }

                );
            _dictionary.TryAdd(
                new Guid("e2c3a01a-b89c-442c-ba2b-12177b3ae1c0"),
                new Booking()
                {
                    Id = new Guid("e2c3a01a-b89c-442c-ba2b-12177b3ae1c0"),
                    EventId = new Guid("bbf1cb27-02af-4253-927b-2aece4724434"),
                    Status = BookingStatus.Confirmed,
                    CreatedAt = new DateTime(2026, 01, 01, 09, 00, 00),
                    ProcessedAt = new DateTime(2026, 01, 01, 09, 10, 00)
                });
            
            _eventRepoMock = new Mock<IRepository<EventEntity>>();
            _eventRepoMock
                .Setup(repo => repo
                    .GetAll())
                .Returns(_eventsList);
            _eventRepoMock
                .Setup(repo => repo
                    .GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns((Guid id, CancellationToken ct) => _eventsList.FirstOrDefault(x => x.Id == id));
            _eventRepoMock
                .Setup(repo => repo
                    .Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Callback((Guid id, CancellationToken ct) => _eventsList.RemoveAll(x => x.Id == id));

            _mockRepository = new Mock<IRepository<Booking>>();
            _mockRepository.Setup(repo => repo
                    .GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns((Guid id, CancellationToken ct) => _dictionary.GetValueOrDefault(id));
            _mockRepository.Setup(repo => repo
                    .Add(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
                .Callback((Booking id, CancellationToken ct) => _dictionary.TryAdd(id.Id, id));

            _mockBookingQueue = new Mock<IQueueSource<Guid>>();
            _mockBookingQueue
                .Setup(q => q.Enqueue(It.IsAny<Guid>()))
                .Callback((Guid id) => _testQueue.Enqueue(id));
            _mockBookingQueue
                .Setup(q => q.TryDequeue(out It.Ref<Guid>.IsAny))
                .Returns((out Guid id) =>
                {
                    return _testQueue.TryDequeue(out id);
                });
            _eventService = new EventService(_eventRepoMock.Object);
            _bookingService = new BookingService(_mockRepository.Object, _mockBookingQueue.Object, _eventService, new NullLogger<BookingService>());
        }

        [Fact]
        public async Task CreateBookingAsync_WithValidEvent_ShouldReturnCreatedBookingWithPendingStaus()
        {
            // Arrange
            Guid expectedId = new Guid("9ab82324-d774-42fd-a2a8-58dcfe22a174");
            BookingStatus expectedStatus = BookingStatus.Pending;
            BookingCreateDto CreateDto = new BookingCreateDto(expectedId);

            // Act
            var result = await _bookingService.CreateBookingAsync(CreateDto, CancellationToken.None);

            // Assert
            Assert.Equal(expectedStatus, result.Status);
        }

        [Fact]
        public async Task CreateBookingAsync_MultipleBookingsForSameEvent_ShouldAssignUniqueIds()
        {
            // Arrange
            Guid expectedId = new Guid("9ab82324-d774-42fd-a2a8-58dcfe22a174");
            BookingStatus expectedStatus = BookingStatus.Pending;
            BookingCreateDto CreateDto = new BookingCreateDto(expectedId);
            int count = 10;
            var results = new List<BookingResponseDto>();

            // Act
            for (int i = 0; i < count; i++)
            {
                results.Add(await _bookingService.CreateBookingAsync(CreateDto, CancellationToken.None));
            }

            // Assert
            var uniqueIdsCount = results.Select(r => r.Id).Distinct().Count();
            Assert.Equal(count, uniqueIdsCount);

            var expectedUniqueEventCount = results.Select(r => r.EventId == expectedId).Distinct().Count();
            Assert.Equal(1, expectedUniqueEventCount);
        }

        [Fact]
        public async Task GetBookingByIdAsync_WithValidBookingId_ShouldRetrieveBookingSuccessfully()
        {
            // Arrange
            Guid expectedId = new Guid("5e68cc94-9dd9-44ae-8ea4-2652f55589ee");
            BookingStatus expectedStatus = BookingStatus.Pending;
            var expectedCreatedAt = new DateTime(2026, 01, 01, 08, 00, 00);
            var expectedEventId = new Guid("9ab82324-d774-42fd-a2a8-58dcfe22a174");

            // Act
            var result = await _bookingService.GetBookingByIdAsync(expectedId, CancellationToken.None);

            // Assert
            Assert.Equal(expectedId, result.Id);
            Assert.Equal(expectedEventId, result.EventId);
            Assert.Equal(expectedStatus, result.Status);
            Assert.Equal(expectedCreatedAt, result.CreatedAt);
        }

        [Fact]
        public async Task BookingProcessingService_WhenCancellationRequested_ShouldThrowOperationCanceledException()
        {
            string expectedExceptionMessage = $"The operation was canceled.";
            var cancellationToken = new CancellationTokenSource();
            cancellationToken.Cancel();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _bookingService.GetBookingForProcessing(cancellationToken.Token));

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            Assert.Equal(cancellationToken.Token, exception.CancellationToken);
            _mockRepository.Verify(r => r.Update(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task BookingProcessingService_ShouldChangeStatusAndSetProcessedAt()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            Guid expectedId = new Guid("bbf1cb27-02af-4253-927b-2aece4724434");
            BookingStatus expectedStatus = BookingStatus.Pending;
            BookingCreateDto CreateDto = new BookingCreateDto(expectedId);
            var validStatuses = new[] { BookingStatus.Confirmed, BookingStatus.Rejected };

            // Act & Assert
            var resultBeforeUpdateStatus = await _bookingService.CreateBookingAsync(CreateDto, CancellationToken.None);
            Assert.Equal(expectedStatus, resultBeforeUpdateStatus.Status);
            Assert.Null(resultBeforeUpdateStatus.ProcessedAt);

            await _bookingService.GetBookingForProcessing(CancellationToken.None);
            var resultAfterUpdateStatus = await _bookingService.GetBookingByIdAsync(resultBeforeUpdateStatus.Id, CancellationToken.None);

            // Assert
            Assert.Contains(resultAfterUpdateStatus.Status, validStatuses);
            Assert.NotNull(resultAfterUpdateStatus.ProcessedAt);
            _mockRepository.Verify(r => r.Update(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateBookingAsync_NonExistingEventId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            Guid expectedId = new Guid("d37e421e-c2a3-456a-a283-dc34d47e183c");
            string expectedExceptionMessage = $"Событие с ID {expectedId} не найдено.";
            BookingCreateDto createDto = new BookingCreateDto(expectedId);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _bookingService.CreateBookingAsync(createDto, CancellationToken.None)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            _eventRepoMock.Verify(r => r.GetById(expectedId), Times.Once);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenEventDoesNotExist_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            Guid existEventId = new Guid("9ab82324-d774-42fd-a2a8-58dcfe22a174");
            string expectedExceptionMessage = $"Событие с ID {existEventId} не найдено.";
            BookingCreateDto createDto = new BookingCreateDto(existEventId);

            // Act
            await _eventService.DeleteEvent(existEventId);
            
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _bookingService.CreateBookingAsync(createDto, CancellationToken.None)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            _eventRepoMock.Verify(r => r.GetById(existEventId), Times.AtLeastOnce);
        }

        [Fact]
        public async Task GetBookingByIdAsync_NonExistingBookingId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            Guid expectedId = new Guid("8f74a67e-f930-4883-a17c-e8067b5bd820");
            string expectedExceptionMessage = $"Бронь с ID {expectedId} не найдена.";

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _bookingService.GetBookingByIdAsync(expectedId, CancellationToken.None)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            _mockRepository.Verify(r => r.GetById(expectedId), Times.Once);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
        {
            // Arrange
            Guid existEventId = new Guid("9ab82324-d774-42fd-a2a8-58dcfe22a174");
            BookingCreateDto createDto = new BookingCreateDto(existEventId);
            string expectedExceptionMessage = $"The operation was canceled.";
            var cancellationToken = new CancellationTokenSource();
            cancellationToken.Cancel();

            // Act
            var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _bookingService.CreateBookingAsync(createDto, cancellationToken.Token)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            Assert.Equal(cancellationToken.Token, exception.CancellationToken);
        }

        [Fact]
        public async Task GetBookingByIdAsync_NonExistingBookingId_ShouldThrowOperationCanceledException()
        {
            // Arrange
            Guid expectedId = new Guid("8f74a67e-f930-4883-a17c-e8067b5bd820");
            string expectedExceptionMessage = $"The operation was canceled.";
            var cancellationToken = new CancellationTokenSource();
            cancellationToken.Cancel();

            // Act
            var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _bookingService.GetBookingByIdAsync(expectedId, cancellationToken.Token)
            );

            //Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            Assert.Equal(cancellationToken.Token, exception.CancellationToken);
        }

    }
}
