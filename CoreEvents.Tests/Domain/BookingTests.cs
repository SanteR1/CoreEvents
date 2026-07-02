using CoreEvents.Domain.Entities;
using CoreEvents.Domain.Enums;
using FluentAssertions;

namespace CoreEvents.Tests.Domain
{
    public class BookingTests
    {
        [Fact]
        public void Confirm_ShouldChangeStatusToConfirmed()
        {
            // Arrange
            var booking = Booking.Create(Guid.NewGuid());

            // Act & Assert
            booking.ProcessedAt.Should().BeNull();
            booking.Confirm();
            booking.Status.Should().Be(BookingStatus.Confirmed);
            booking.ProcessedAt.Should().NotBeNull();
        }

        [Fact]
        public void Reject_ShouldChangeStatusToRejected()
        {
            // Arrange
            var booking = Booking.Create(Guid.NewGuid());

            // Act & Assert
            booking.ProcessedAt.Should().BeNull();
            booking.Reject();
            booking.Status.Should().Be(BookingStatus.Rejected);
            booking.ProcessedAt.Should().NotBeNull();
        }
    }
}
