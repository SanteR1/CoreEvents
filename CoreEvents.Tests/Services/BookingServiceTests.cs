using CoreEvents.Middleware;
using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;
using CoreEvents.Services.Implementations;
using CoreEvents.Tests.Infrastructure;
using Moq;

namespace CoreEvents.Tests.Services
{
    public class BookingServiceTests
    {
        private readonly TestContext _ctx;
        private readonly BookingService _bookingService;
        private readonly EventService _eventService;

        public BookingServiceTests()
        {
            _ctx = new TestContext();
            _ctx.SetupMocks();

            _bookingService = new BookingService(_ctx.BookingRepo.Object, _ctx.EventRepo.Object);
            _eventService = new EventService(_ctx.EventRepo.Object);
        }

        [Fact]
        public async Task CreateBookingAsync_WithValidEvent_ShouldReturnCreatedBookingWithPendingStatus()
        {
            // Arrange
            var eventEntity = _ctx.AddEvent("Event Test");
            BookingCreateDto createDto = new BookingCreateDto(eventEntity.Id);

            // Act
            var result = await _bookingService.CreateBookingAsync(createDto, default);

            // Assert
            Assert.Equal(BookingStatus.Pending, result.Status);
            Assert.Equal(eventEntity.Id, result.EventId);
        }

        [Fact]
        public async Task CreateBookingAsync_MultipleBookingsForSameEvent_ShouldAssignUniqueIds()
        {
            // Arrange
            const int initialSeats = 10;
            var eventEntity = _ctx.AddEvent("Multi Event", seats: initialSeats);
            BookingCreateDto createDto = new BookingCreateDto(eventEntity.Id);
            int count = 10;

            // Act
            var results = await Task.WhenAll(
                Enumerable.Range(0, count)
                    .Select(_ => Task.Run(() => _bookingService.CreateBookingAsync(createDto, default))));

            // Assert
            var uniqueIdsCount = results.Select(r => r.Id).Distinct().Count();
            Assert.Equal(initialSeats, uniqueIdsCount);

            Assert.All(results, b =>
                Assert.Equal(eventEntity.Id, b.EventId));

            Assert.Equal(0, eventEntity.AvailableSeats);
        }

        [Fact]
        public async Task CreateBookingAsync_MultipleBookingsForSameEvent_ShouldAssignStatusIsPending()
        {
            // Arrange
            var booking = _ctx.AddEvent("Multi Event", seats: 10);
            BookingCreateDto createDto = new BookingCreateDto(booking.Id);
            int count = 10;

            // Act
            var results = await Task.WhenAll(
                Enumerable.Range(0, count)
                    .Select(_ => Task.Run(() => _bookingService.CreateBookingAsync(createDto, default))));

            // Assert
            Assert.All(results, r => Assert.Equal(BookingStatus.Pending, r.Status));
        }

        [Fact]
        public async Task GetBookingByIdAsync_WithValidBookingId_ShouldRetrieveSuccessfully()
        {
            // Arrange
            var booking = _ctx.AddBooking();

            // Act
            var result = await _bookingService.GetBookingByIdAsync(booking.Id, default);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(booking.Id, result.Id);
            Assert.Equal(booking.Status, result.Status);
        }

        [Fact]
        public async Task CreateBookingAsync_NonExistingEventId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            BookingCreateDto createDto = new BookingCreateDto(Guid.NewGuid());
            string expectedExceptionMessage = $"Событие с ID {createDto.EventId} не найдено.";

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _bookingService.CreateBookingAsync(createDto, default)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            _ctx.EventRepo.Verify(r => r.GetById(createDto.EventId), Times.Once);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenEventWasDeleted_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var eventEntity = _ctx.AddEvent("To Be Deleted");
            _ctx.AddBooking(eventEntity.Id);
            string expectedExceptionMessage = $"Событие с ID {eventEntity.Id} не найдено.";
            BookingCreateDto createDto = new BookingCreateDto(eventEntity.Id);

            // Act
            await _eventService.DeleteEvent(eventEntity.Id);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _bookingService.CreateBookingAsync(createDto, default)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            _ctx.BookingRepo.Verify(r => r.Add(It.IsAny<Booking>(), default), Times.Never);
        }

        [Fact]
        public async Task GetBookingByIdAsync_NonExistingBookingId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            _ctx.AddBooking();
            var nonExistBooking = Guid.NewGuid();
            string expectedExceptionMessage = $"Бронь с ID {nonExistBooking} не найдена.";

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _bookingService.GetBookingByIdAsync(nonExistBooking, default)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            _ctx.BookingRepo.Verify(r => r.GetById(nonExistBooking), Times.Once);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var booking = _ctx.AddBooking();
            BookingCreateDto createDto = new BookingCreateDto(booking.Id);
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
            var booking = _ctx.AddBooking();
            string expectedExceptionMessage = $"The operation was canceled.";
            var cancellationToken = new CancellationTokenSource();
            cancellationToken.Cancel();

            // Act
            var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _bookingService.GetBookingByIdAsync(booking.Id, cancellationToken.Token)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            Assert.Equal(cancellationToken.Token, exception.CancellationToken);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenBookingCreated_ShouldDecreaseAvailableSeats()
        {
            // Arrange
            const int initialSeats = 3;
            var eventEntity = _ctx.AddEvent("Test Event", seats: initialSeats);
            BookingCreateDto createDto = new BookingCreateDto(eventEntity.Id);
            const int expectedSeats = initialSeats - 1;

            // Act
            await _bookingService.CreateBookingAsync(createDto, default);

            // Assert
            Assert.Equal(expectedSeats, eventEntity.AvailableSeats);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenSeatsAreDepleted_ShouldAllowSuccessfulBookingsUntilEmpty()
        {
            // Arrange
            const int initialSeats = 2;
            var eventEntity = _ctx.AddEvent("Test Event", seats: initialSeats);
            var createDto = new BookingCreateDto(eventEntity.Id);

            // Act & Assert
            await _bookingService.CreateBookingAsync(createDto, default);
            Assert.Equal(initialSeats - 1, eventEntity.AvailableSeats);

            // Act & Assert
            await _bookingService.CreateBookingAsync(createDto, default);
            Assert.Equal(0, eventEntity.AvailableSeats);

            // Act & Assert
            await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
                _bookingService.CreateBookingAsync(createDto, default));
        }

        [Fact]
        public async Task CreateBookingAsync_WhenNoSeatsAvailable_ShouldThrowNoAvailableSeatsException()
        {
            // Arrange
            const int initialSeats = 1;
            const string expectedMessage = "No available seats for this event.";
            var eventEntity = _ctx.AddEvent("Test Event", seats: initialSeats);
            var createDto = new BookingCreateDto(eventEntity.Id);
            
            eventEntity.TryReserveSeats(1);

            // Assert
            Assert.Equal(0, eventEntity.AvailableSeats);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
                _bookingService.CreateBookingAsync(createDto, default));

            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenMultipleConcurrentRequests_ShouldPreventOverbooking()
        {
            // Arrange
            const int initialSeats = 5;
            const int totalRequests = 20;
            const int expectedSuccesses = 5;


            var eventEntity = _ctx.AddEvent("Concurrency Test", seats: initialSeats);
            BookingCreateDto createDto = new BookingCreateDto(eventEntity.Id);


            var tasks = Enumerable.Range(0, totalRequests)
                .Select(_ => Task.Run(() => _bookingService.CreateBookingAsync(createDto, default)))
                .ToArray();

            var allTasks = Task.WhenAll(tasks);

            try
            {
                await allTasks;
            }
            catch { }

            // Assert
            int successCount = tasks.Count(t => t.Status == TaskStatus.RanToCompletion);
            var exceptions = allTasks.Exception?.InnerExceptions ?? Enumerable.Empty<Exception>();

            int noSeatsExceptionCount = exceptions.Count(e => e is NoAvailableSeatsException);
            int otherErrorCount = exceptions.Count(e => e is not NoAvailableSeatsException);


            Assert.Equal(expectedSuccesses, successCount);
            Assert.Equal(totalRequests - expectedSuccesses, noSeatsExceptionCount);
            Assert.Equal(0, otherErrorCount);

            Assert.Equal(0, eventEntity.AvailableSeats);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenMultipleConcurrentRequests_ShouldAssignUniqueIds()
        {
            // Arrange
            const int initialSeats = 10;
            const int totalRequests = 10;
            const int expectedSuccesses = 10;


            var eventEntity = _ctx.AddEvent("Concurrency Test", seats: initialSeats);
            BookingCreateDto createDto = new BookingCreateDto(eventEntity.Id);

            // Act
            var results = await Task.WhenAll(
                Enumerable.Range(0, totalRequests)
                    .Select(_ => Task.Run(() => _bookingService.CreateBookingAsync(createDto, default))));

            // Assert
            var uniqueIdsCount = results.Select(r => r.Id).Distinct().Count();
            Assert.Equal(expectedSuccesses, uniqueIdsCount);

            Assert.All(results, b =>
                Assert.Equal(eventEntity.Id, b.EventId));

            Assert.Equal(0, eventEntity.AvailableSeats);
        }
    }
}
