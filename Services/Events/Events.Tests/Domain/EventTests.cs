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
        var now = DateTimeOffset.UtcNow;

        // Act
        eventEntity.Cancel(now);

        // Assert
        eventEntity.IsActive.Should().BeFalse();
        eventEntity.Version.Should().Be(initialVersion + 1);
    }

    [Fact]
    public void TryReserveSeats_WhenEventIsInactive_ShouldReturnRejected()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var eventEntity = TestEventFactory.Create(seats: 10);
        eventEntity.Cancel(now);

        // Act
        var result = eventEntity.TryReserveSeats(Guid.NewGuid(), 1, now);

        // Assert
        result.Should().Be(Events.Domain.Enums.ReserveSeatsResult.Rejected);
        eventEntity.AvailableSeats.Should().Be(10);
    }

    [Fact]
    public void TryReserveSeats_WhenSeatsAvailable_ShouldDecreaseAvailableSeats()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        int seats = 10;
        int reserveSeats = 1;
        int availableSeats = 9;
        var eventEntity = TestEventFactory.Create(seats: seats);

        // Act
        var result = eventEntity.TryReserveSeats(Guid.NewGuid(), reserveSeats, now);

        // Assert
        result.Should().Be(Events.Domain.Enums.ReserveSeatsResult.Reserved);
        eventEntity.AvailableSeats.Should().Be(availableSeats);
    }

    [Fact]
    public void TryReserveSeats_ExactlyAllAvailableSeats_ShouldSetAvailableSeatsToZero()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var eventEntity = TestEventFactory.Create(seats: 5);

        // Act
        var result = eventEntity.TryReserveSeats(Guid.NewGuid(), 5, now);

        // Assert
        result.Should().Be(Events.Domain.Enums.ReserveSeatsResult.Reserved);
        eventEntity.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public void TryReserveSeats_WhenNoSeatsLeft_ShouldReturnRejectedAndNotChangeAvailableSeats()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        int seats = 1;
        var eventEntity = TestEventFactory.Create(seats: seats);
        eventEntity.TryReserveSeats(Guid.NewGuid(), 1, now);

        // Act
        var result = eventEntity.TryReserveSeats(Guid.NewGuid(), 1, now);

        // Assert
        result.Should().Be(Events.Domain.Enums.ReserveSeatsResult.Rejected);
        eventEntity.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public void TryReserveSeats_WhenRequestExceedsAvailable_ShouldReturnRejectedAndKeepAvailableSeatsUnchanged()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        int seats = 5;
        var eventEntity = TestEventFactory.Create(seats: seats);

        // Act
        var result = eventEntity.TryReserveSeats(Guid.NewGuid(), 6, now);

        // Assert
        result.Should().Be(Events.Domain.Enums.ReserveSeatsResult.Rejected);
        eventEntity.AvailableSeats.Should().Be(seats);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TryReserveSeats_WithNonPositiveCount_ShouldThrowArgumentOutOfRangeException(int count)
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var eventEntity = TestEventFactory.Create(seats: 5);

        // Act
        Action act = () => eventEntity.TryReserveSeats(Guid.NewGuid(), count, now);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(count));
    }

    [Fact]
    public void ReleaseSeats_WithValidAmount_ShouldIncreaseAvailableSeats()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        int seats = 10;
        var eventEntity = TestEventFactory.Create(seats: seats);
        var bookingId = Guid.NewGuid();
        eventEntity.TryReserveSeats(bookingId, 2, now);

        // Act
        var result = eventEntity.ReleaseSeats(bookingId, 1, now);

        // Assert
        result.Should().BeTrue();
        eventEntity.AvailableSeats.Should().Be(9);
    }

    [Fact]
    public void ReleaseSeats_WhenNothingWasReserved_ShouldReturnFalseAndNotExceedTotalSeats()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        int seats = 10;
        var eventEntity = TestEventFactory.Create(seats: seats);

        // Act
        var result = eventEntity.ReleaseSeats(Guid.NewGuid(), 1, now);

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
        var now = DateTimeOffset.UtcNow;
        var eventEntity = TestEventFactory.Create(seats: 5);

        // Act
        Action act = () => eventEntity.ReleaseSeats(Guid.NewGuid(), count, now);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(count));
    }
}
