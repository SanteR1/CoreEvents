using System;
using System.Collections.Generic;
using System.Text;
using CoreEvents.Models.Domain;

namespace CoreEvents.Tests.Domain
{
    public class BookingTests
    {
        [Fact]
        public void Confirm_ShouldChangeStatusToConfirmed()
        {
            // Arrange
            var booking = Booking.Create(Guid.NewGuid());

            Assert.Null(booking.ProcessedAt);

            // Act
            booking.Confirm();

            // Assert
            Assert.Equal(BookingStatus.Confirmed, booking.Status);
            Assert.IsType<DateTime>(booking.ProcessedAt);
        }

        [Fact]
        public void Reject_ShouldChangeStatusToRejected()
        {
            // Arrange
            var booking = Booking.Create(Guid.NewGuid());

            Assert.Null(booking.ProcessedAt);

            // Act
            booking.Reject();

            // Assert
            Assert.Equal(BookingStatus.Rejected, booking.Status);
            Assert.IsType<DateTime>(booking.ProcessedAt);
        }

    }
}
