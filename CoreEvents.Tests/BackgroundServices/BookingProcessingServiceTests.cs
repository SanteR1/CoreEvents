using System.Collections.Concurrent;
using CoreEvents.Data.Repositories;
using CoreEvents.Infrastructure.BackgroundServices;
using CoreEvents.Models.Domain;
using CoreEvents.Services.Implementations;
using CoreEvents.Services.Interfaces;
using CoreEvents.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CoreEvents.Tests.BackgroundServices
{
    public class BookingProcessingServiceTests
    {
        private readonly BookingProcessingService _bookingProcessingService;
        private readonly TestContext _ctx;

        public BookingProcessingServiceTests()
        {
            _ctx = new TestContext();
            _ctx.SetupMocks();
            _bookingProcessingService = new BookingProcessingService(_ctx.EventRepo.Object, _ctx.BookingRepo.Object,
                new NullLogger<BookingProcessingService>());
            _bookingProcessingService.ProcessingDelaySeconds = 0;
        }

        [Fact]
        public async Task BookingProcessingService_WhenCancellationRequested_ShouldThrowTaskCanceledException()
        {
            // Arrange
            var booking = _ctx.AddBooking();
            string expectedExceptionMessage = "A task was canceled.";
            var cancellationToken = new CancellationTokenSource();
            await cancellationToken.CancelAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await _bookingProcessingService.ProcessBookingAsync(booking, cancellationToken.Token));

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            Assert.Equal(cancellationToken.Token, exception.CancellationToken);
            _ctx.BookingRepo.Verify(r => r.Update(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task BookingProcessingService_ShouldChangeStatusAndSetProcessedAt()
        {
            // Arrange
            EventEntity eventEntity = _ctx.AddEvent("Test Event");
            Booking booking = _ctx.AddBooking(eventEntity.Id);
            BookingStatus expectedStatus = BookingStatus.Pending;
            var validStatuses = new[] { BookingStatus.Confirmed, BookingStatus.Rejected };

            // Act & Assert
            var resultBeforeUpdateStatus = booking;
            Assert.Equal(expectedStatus, resultBeforeUpdateStatus.Status);
            Assert.Null(resultBeforeUpdateStatus.ProcessedAt);

            await _bookingProcessingService.ProcessBookingAsync(resultBeforeUpdateStatus, CancellationToken.None);
            var resultAfterUpdateStatus = _ctx.BookingRepo.Object.GetById(resultBeforeUpdateStatus.Id, CancellationToken.None);

            // Assert
            Assert.NotNull(resultAfterUpdateStatus?.ProcessedAt);
            Assert.Contains(resultAfterUpdateStatus.Status, validStatuses);
            Assert.Equal(booking.EventId, resultBeforeUpdateStatus.EventId);
            _ctx.BookingRepo.Verify(r => r.Update(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
