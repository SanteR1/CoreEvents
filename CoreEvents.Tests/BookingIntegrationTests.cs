using System;
using System.Collections.Generic;
using System.Text;
using CoreEvents.Infrastructure.BackgroundServices;
using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;
using CoreEvents.Services.Implementations;
using CoreEvents.Services.Interfaces;
using CoreEvents.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreEvents.Tests
{
    public class BookingIntegrationTests
    {
        private readonly TestContext _ctx;
        private readonly BookingService _bookingService;
        private readonly BookingProcessingService _bookingProcessingService;
        public BookingIntegrationTests()
        {
            _ctx = new TestContext();
            _ctx.SetupMocks();

            _bookingService = new BookingService(_ctx.BookingRepo.Object, _ctx.EventRepo.Object);
            _bookingProcessingService = new BookingProcessingService(_ctx.EventRepo.Object, _ctx.BookingRepo.Object,
                new NullLogger<BookingProcessingService>());
            _bookingProcessingService.ProcessingDelaySeconds = 0;
        }

        [Fact]
        public async Task FullWorkflow_AfterRejectingBooking_ShouldAllowNewBookingOnSameEvent()
        {
            const int initialSeats = 1;
            var eventEntity = _ctx.AddEvent("Integration Test", seats: initialSeats);
            var createDto = new BookingCreateDto(eventEntity.Id);

            var firstBookingDto = await _bookingService.CreateBookingAsync(createDto, default);
            Assert.Equal(0, eventEntity.AvailableSeats);


            var firstBookingEntity = _ctx.Bookings[firstBookingDto.Id];
            Assert.Equal(BookingStatus.Pending, firstBookingEntity.Status);

            await _bookingProcessingService.HandleRejectionAsync(firstBookingEntity, eventEntity, default);

            Assert.Equal(BookingStatus.Rejected, firstBookingEntity.Status);
            Assert.Equal(1, eventEntity.AvailableSeats);

            var secondBookingDto = await _bookingService.CreateBookingAsync(createDto, default);

            Assert.NotNull(secondBookingDto);
            Assert.Equal(eventEntity.Id, secondBookingDto.EventId);
            Assert.Equal(0, eventEntity.AvailableSeats);
        }
    }
}
