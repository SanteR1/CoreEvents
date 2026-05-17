using System;
using System.Collections.Concurrent;
using CoreEvents.Data.Repositories;
using CoreEvents.Models.Domain;
using Moq;

namespace CoreEvents.Tests.Infrastructure
{
    public class TestContext
    {
        public List<EventEntity> Events { get; } = [];
        public ConcurrentDictionary<Guid, Booking> Bookings { get; } = new();
        public Mock<IRepository<EventEntity>> EventRepo { get; } = new();
        public Mock<IBookingRepository> BookingRepo { get; } = new();

        public void SetupMocks()
        {
            // Setup Event Repository
            EventRepo.Setup(r => r.GetAll()).Returns(Events);
            EventRepo.Setup(r => r.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns((Guid id, CancellationToken ct) => Events.FirstOrDefault(x => x.Id == id));
            EventRepo.Setup(r => r.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Callback((Guid id, CancellationToken ct) => Events.RemoveAll(x => x.Id == id));

            // Setup Booking Repository
            BookingRepo.Setup(r => r.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns((Guid id, CancellationToken ct) => Bookings.GetValueOrDefault(id));
            BookingRepo.Setup(r => r.Add(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
                .Callback((Booking b, CancellationToken ct) => Bookings.TryAdd(b.Id, b));
            BookingRepo.Setup(r => r.Update(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
                .Callback((Booking b, CancellationToken ct) => Bookings[b.Id] = b);
        }

        public EventEntity AddEvent(
            string title,
            string? description = "Test Description",
            DateTime? startAt = null,
            DateTime? endAt = null,
            int seats = 1
            )
        {
            var eventEntity = TestDataFactory.CreateEvent(title, description, startAt, endAt,  seats);
            Events.Add(eventEntity);
            return eventEntity;
        }

        public Booking AddBooking(Guid? eventId = default)
        {
            var booking = new Booking()
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.Now,
                EventId = eventId ?? Guid.NewGuid(),
                Status = BookingStatus.Pending
            };
            Bookings.TryAdd(booking.Id, booking);
            return booking;
        }
    }
}
