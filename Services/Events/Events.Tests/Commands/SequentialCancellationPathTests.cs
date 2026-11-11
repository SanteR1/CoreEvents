using AwesomeAssertions;
using Events.Application.Abstractions;
using Events.Application.Abstractions.Repositories;
using Events.Application.Commands;
using Events.Domain.Entities;
using Events.Domain.Enums;
using Events.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Events.Tests.Commands;

public class SequentialCancellationPathTests
{
    [Fact]
    public async Task Handle_SequentialCallsOnBothPaths_ShouldReleaseSeatsExactlyOnce()
    {
        // Arrange
        var @event = TestEventFactory.Create(seats: 10);
        @event.TryReserveSeats(3);
        @event.AvailableSeats.Should().Be(7);

        var bookingId = Guid.NewGuid();
        var reservation = SeatReservation.CreateReserved(
            bookingId, @event.Id, 3, 100m, "KZT", 0m, 1L,
            DateTimeOffset.UtcNow.AddMinutes(15), bookingId, Guid.NewGuid());

        var eventsMock = new Mock<IEventRepository>();
        var reservationsMock = new Mock<ISeatReservationRepository>();
        var outboxMock = new Mock<IOutboxService>();

        reservationsMock.Setup(r => r.GetByBookingIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        eventsMock.Setup(e => e.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var correlationContextMock = new Mock<ICorrelationContext>();

        var releaseHandler = new BookingReservationReleaseRequestHandler(
            eventsMock.Object,
            reservationsMock.Object,
            outboxMock.Object,
            correlationContextMock.Object);

        var syncCancelledHandler = new SyncBookingCancelledHandler(
            eventsMock.Object,
            reservationsMock.Object,
            NullLogger<SyncBookingCancelledHandler>.Instance);

        // 1. Вызов основного пути BookingReservationReleaseRequestCommand
        var releaseCmd = new BookingReservationReleaseRequestCommand(bookingId, @event.Id, 3, CoreEvents.Shared.Contracts.Events.CancellationReason.UserCancelled);
        await releaseHandler.Handle(releaseCmd, CancellationToken.None);

        reservation.Status.Should().Be(SeatsReservationStatus.Released);
        @event.AvailableSeats.Should().Be(10, "Места должны быть возвращены первым вызовом");

        // 2. Последующий вызов пути SyncBookingCancelledCommand
        var syncCmd = new SyncBookingCancelledCommand(bookingId, @event.Id);
        await syncCancelledHandler.Handle(syncCmd, CancellationToken.None);

        // Assert: места не должны вернуться второй раз!
        @event.AvailableSeats.Should().Be(10, "Повторный вызов другого пути не должен приводить к двойному возврату мест");
    }
}
