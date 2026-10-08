using AwesomeAssertions;
using Events.Domain.Exceptions;
using Events.Tests.Infrastructure;

namespace Events.Tests.Domain;

public class EventTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Act
        var eventEntity = TestEventFactory.Create(price: 500m, currency: "USD");

        // Assert
        eventEntity.Price.Should().Be(500m);
        eventEntity.Currency.Should().Be("USD");
        eventEntity.PriceVersion.Should().Be(1L);
        eventEntity.Version.Should().Be(1L);
        eventEntity.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithNegativePrice_ShouldThrowValidationException()
    {
        // Act
        Action act = () => TestEventFactory.Create(price: -10m);

        // Assert
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Create_WithZeroOrNegativeSeats_ShouldThrowValidationException()
    {
        // Act
        Action act = () => TestEventFactory.Create(seats: 0);

        // Assert
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Update_WithMetadataChangeOnly_ShouldIncrementVersionOnly()
    {
        // Arrange
        var eventEntity = TestEventFactory.Create(price: 100m, currency: "KZT");
        var initialPriceVersion = eventEntity.PriceVersion;
        var initialVersion = eventEntity.Version;

        // Act
        eventEntity.Update(
            title: "New Title",
            description: "New Description",
            startAt: DateTime.UtcNow.AddDays(2),
            endAt: DateTime.UtcNow.AddDays(2).AddHours(2),
            price: 100m,
            currency: "KZT");

        // Assert
        eventEntity.Title.Should().Be("New Title");
        eventEntity.Version.Should().Be(initialVersion + 1);
        eventEntity.PriceVersion.Should().Be(initialPriceVersion);
    }

    [Fact]
    public void Update_WithPriceChange_ShouldIncrementBothVersionAndPriceVersion()
    {
        // Arrange
        var eventEntity = TestEventFactory.Create(price: 100m, currency: "KZT");
        var initialPriceVersion = eventEntity.PriceVersion;
        var initialVersion = eventEntity.Version;

        // Act
        eventEntity.Update(
            title: eventEntity.Title,
            description: eventEntity.Description,
            startAt: eventEntity.StartAt,
            endAt: eventEntity.EndAt,
            price: 250m,
            currency: "KZT");

        // Assert
        eventEntity.Price.Should().Be(250m);
        eventEntity.Version.Should().Be(initialVersion + 1);
        eventEntity.PriceVersion.Should().Be(initialPriceVersion + 1);
    }

    [Fact]
    public void Update_WithCurrencyChange_ShouldIncrementBothVersionAndPriceVersion()
    {
        // Arrange
        var eventEntity = TestEventFactory.Create(price: 100m, currency: "KZT");
        var initialPriceVersion = eventEntity.PriceVersion;
        var initialVersion = eventEntity.Version;

        // Act
        eventEntity.Update(
            title: eventEntity.Title,
            description: eventEntity.Description,
            startAt: eventEntity.StartAt,
            endAt: eventEntity.EndAt,
            price: 100m,
            currency: "EUR");

        // Assert
        eventEntity.Currency.Should().Be("EUR");
        eventEntity.Version.Should().Be(initialVersion + 1);
        eventEntity.PriceVersion.Should().Be(initialPriceVersion + 1);
    }

    [Fact]
    public void Cancel_ShouldSetIsActiveToFalseAndIncrementVersion()
    {
        // Arrange
        var eventEntity = TestEventFactory.Create();
        var initialVersion = eventEntity.Version;

        // Act
        eventEntity.Cancel();

        // Assert
        eventEntity.IsActive.Should().BeFalse();
        eventEntity.Version.Should().Be(initialVersion + 1);
    }

    [Fact]
    public void TryReserveSeats_WhenEventIsInactive_ShouldReturnFalse()
    {
        // Arrange
        var eventEntity = TestEventFactory.Create(seats: 10);
        eventEntity.Cancel();

        // Act
        var result = eventEntity.TryReserveSeats(1);

        // Assert
        result.Should().BeFalse();
        eventEntity.AvailableSeats.Should().Be(10);
    }

    [Fact]
    public void TryReserveSeats_WhenSeatsAvailable_ShouldDecreaseAvailableSeats()
    {
        // Arrange
        int seats = 10;
        int reserveSeats = 1;
        int availableSeats = 9;
        var eventEntity = TestEventFactory.Create(seats: seats);

        // Act
        var result = eventEntity.TryReserveSeats(reserveSeats);

        // Assert
        result.Should().BeTrue();
        eventEntity.AvailableSeats.Should().Be(availableSeats);
    }

    [Fact]
    public void TryReserveSeats_ExactlyAllAvailableSeats_ShouldSetAvailableSeatsToZero()
    {
        // Arrange
        var eventEntity = TestEventFactory.Create(seats: 5);

        // Act
        var result = eventEntity.TryReserveSeats(5);

        // Assert
        result.Should().BeTrue();
        eventEntity.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public void TryReserveSeats_WhenNoSeatsLeft_ShouldReturnFalseAndNotChangeAvailableSeats()
    {
        // Arrange
        int seats = 1;
        var eventEntity = TestEventFactory.Create(seats: seats);
        eventEntity.TryReserveSeats(1);

        // Act
        var result = eventEntity.TryReserveSeats(1);

        // Assert
        result.Should().BeFalse();
        eventEntity.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public void TryReserveSeats_WhenRequestExceedsAvailable_ShouldReturnFalseAndKeepAvailableSeatsUnchanged()
    {
        // Arrange
        int seats = 5;
        var eventEntity = TestEventFactory.Create(seats: seats);

        // Act
        var result = eventEntity.TryReserveSeats(6);

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
        var eventEntity = TestEventFactory.Create(seats: 5);

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
        var eventEntity = TestEventFactory.Create(seats: seats);
        eventEntity.TryReserveSeats(2);

        // Act
        var result = eventEntity.ReleaseSeats(1);

        // Assert
        result.Should().BeTrue();
        eventEntity.AvailableSeats.Should().Be(9);
    }

    [Fact]
    public void ReleaseSeats_WhenNothingWasReserved_ShouldReturnFalseAndNotExceedTotalSeats()
    {
        // Arrange
        int seats = 10;
        var eventEntity = TestEventFactory.Create(seats: seats);

        // Act
        var result = eventEntity.ReleaseSeats(1);

        // Assert
        result.Should().BeFalse();
        eventEntity.AvailableSeats.Should().Be(10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ReleaseSeats_WithNonPositiveCount_ShouldThrowArgumentOutOfRangeException(int count)
    {
        // Arrange
        var eventEntity = TestEventFactory.Create(seats: 5);

        // Act
        Func<bool> act = () => eventEntity.ReleaseSeats(count);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(count));
    }
}
