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
            var result = await _bookingService.CreateBookingAsync(createDto, CancellationToken.None);

            // Assert
            Assert.Equal(BookingStatus.Pending, result.Status);
            Assert.Equal(eventEntity.Id, result.EventId);
        }

        [Fact]
        public async Task CreateBookingAsync_MultipleBookingsForSameEvent_ShouldAssignUniqueIds()
        {
            // Arrange
            var booking = _ctx.AddEvent("Multi Event", seats: 10);
            BookingCreateDto createDto = new BookingCreateDto(booking.Id);
            int count = 10;

            // Act
            var results = await Task.WhenAll(
                Enumerable.Range(0, count)
                    .Select(_ => _bookingService.CreateBookingAsync(createDto, CancellationToken.None)));

            // Assert
            var uniqueIdsCount = results.Select(r => r.Id).Distinct().Count();
            Assert.Equal(count, uniqueIdsCount);

            var uniqueEventCount = results.Select(r => r.EventId == booking.Id).Distinct().Count();
            Assert.Equal(1, uniqueEventCount);
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
                    .Select(_ => _bookingService.CreateBookingAsync(createDto, CancellationToken.None)));

            // Assert
            Assert.All(results, r => Assert.Equal(BookingStatus.Pending, r.Status));
        }

        [Fact]
        public async Task GetBookingByIdAsync_WithValidBookingId_ShouldRetrieveSuccessfully()
        {
            // Arrange
            var booking = _ctx.AddBooking();

            // Act
            var result = await _bookingService.GetBookingByIdAsync(booking.Id, CancellationToken.None);

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
                await _bookingService.CreateBookingAsync(createDto, CancellationToken.None)
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
                await _bookingService.CreateBookingAsync(createDto, CancellationToken.None)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            _ctx.BookingRepo.Verify(r => r.Add(It.IsAny<Booking>(), CancellationToken.None), Times.Never);
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
                await _bookingService.GetBookingByIdAsync(nonExistBooking, CancellationToken.None)
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

    }
}
