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
        public async Task HandleConfirmationAsync_ShouldSetStatusToConfirmedAndSetProcessedAt()
        {
            // Arrange
            var eventEntity = _ctx.AddEvent("Confirm Test", seats: 5);
            var booking = _ctx.AddBooking(eventEntity.Id);

            // Act 
            await _bookingProcessingService.HandleConfirmationAsync(booking, eventEntity, default);

            // Assert
            Assert.Equal(BookingStatus.Confirmed, booking.Status);
            Assert.NotEqual(default, booking.ProcessedAt);
        }
        [Fact]
        public async Task HandleRejectionAsync_WhenEventExists_ShouldSetStatusToRejected()
        {
            // Arrange
            var eventEntity = _ctx.AddEvent("Reject Test", seats: 1);
            var booking = _ctx.AddBooking(eventEntity.Id);

            eventEntity.TryReserveSeats(1);
            Assert.Equal(0, eventEntity.AvailableSeats);

            // Act
            await _bookingProcessingService.HandleRejectionAsync(booking, eventEntity, default);

            // Assert
            Assert.Equal(BookingStatus.Rejected, booking.Status);
            Assert.NotEqual(default, booking.ProcessedAt);
        }

        [Fact]
        public async Task HandleRejectionAsync_WhenEventExists_ShouldReleaseSeats()
        {
            // Arrange
            var eventEntity = _ctx.AddEvent("Reject Test", seats: 1);
            var booking = _ctx.AddBooking(eventEntity.Id);

            eventEntity.TryReserveSeats(1);
            Assert.Equal(0, eventEntity.AvailableSeats);

            // Act
            await _bookingProcessingService.HandleRejectionAsync(booking, eventEntity, default);

            // Assert
            Assert.Equal(1, eventEntity.AvailableSeats);
        }

        [Fact]
        public async Task HandleRejectionAsync_WhenEventDoesNotExist_ShouldSetStatusToRejectedButNotReleaseSeats()
        {
            // Arrange
            var booking = _ctx.AddBooking(Guid.NewGuid());
            booking.Status = BookingStatus.Pending;

            // Act
            await _bookingProcessingService.HandleRejectionAsync(booking, null, default);

            // Assert
            Assert.Equal(BookingStatus.Rejected, booking.Status);
        }
    }
}
