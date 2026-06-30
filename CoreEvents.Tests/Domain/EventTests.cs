
using CoreEvents.Tests.Infrastructure;
using FluentAssertions;

namespace CoreEvents.Tests.Domain
{
    public class EventTests
    {
        [Fact]
        public void TryReserveSeats_WhenSeatsAvailable_ShouldDecreaseAvailableSeats()
        {
            // Arrange
            int seats = 10;
            int reserveSeats = 1;
            int availableSeats = 9;
            var eventEntity = TestEventFactory.Create(seats: seats);

            // Act
            eventEntity.TryReserveSeats(reserveSeats);

            // Assert
            eventEntity.AvailableSeats.Should().Be(availableSeats);
        }
        [Fact]
        public void TryReserveSeats_WhenNoSeatsLeft_ShouldReturnFalseAndNotChangeAvailableSeats()
        {
            // Arrange
            int seats = 1;
            int reserveSeats = 1;
            int availableSeats = 0;
            var eventEntity = TestEventFactory.Create(seats: seats);
            eventEntity.TryReserveSeats(reserveSeats);

            // Act
            var result = eventEntity.TryReserveSeats(reserveSeats);

            // Assert
            result.Should().BeFalse();
            eventEntity.AvailableSeats.Should().Be(availableSeats);
        }

        [Fact]
        public void TryReserveSeats_WhenRequestExceedsAvailable_ShouldReturnFalseAndKeepAvailableSeatsUnchanged()
        {
            // Arrange
            int seats = 5;
            int reserveSeats = 6;
            var eventEntity = TestEventFactory.Create(seats: seats);

            // Act
            var result = eventEntity.TryReserveSeats(reserveSeats);

            // Assert
            result.Should().BeFalse();
            eventEntity.AvailableSeats.Should().Be(seats);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TryReserveSeats_WithNonPositiveCount_ShouldThrowArgumentOutOfRangeException(int count)
        {
            // Arrange
            int seats = 5;
            var eventEntity = TestEventFactory.Create(seats: seats);

            // Act
            Func<bool> act = () => eventEntity.TryReserveSeats(count);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName(nameof(count));
        }

        [Fact]
        public void ReleaseSeats_WithValidAmount_ShouldIncreaseAvailableSeats()
        {
            // Arrange
            int seats = 10;
            int releaseSeats = 1;
            int reserveSeats = 1;
            int availableSeats = 10;
            var eventEntity = TestEventFactory.Create(seats: seats);
            eventEntity.TryReserveSeats(reserveSeats);

            // Act
            var result =  eventEntity.ReleaseSeats(releaseSeats);

            // Assert
            result.Should().BeTrue();
            eventEntity.AvailableSeats.Should().Be(availableSeats);
        }

        [Fact]
        public void ReleaseSeats_WhenNothingWasReserved_ShouldReturnFalseAndNotExceedTotalSeats()
        {
            // Arrange
            int seats = 10;
            int releaseSeats = 1;
            int availableSeats = 10;
            var eventEntity = TestEventFactory.Create(seats: seats);

            // Act
            var result = eventEntity.ReleaseSeats(releaseSeats);

            // Assert
            result.Should().BeFalse();
            eventEntity.AvailableSeats.Should().Be(availableSeats);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ReleaseSeats_WithNonPositiveCount_ShouldThrowArgumentOutOfRangeException(int count)
        {
            // Arrange
            int seats = 5;
            var eventEntity = TestEventFactory.Create(seats: seats);

            // Act
            Func<bool> act = () => eventEntity.ReleaseSeats(count);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName(nameof(count));
        }
    }
}
