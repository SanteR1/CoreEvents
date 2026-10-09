using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using Events.Application.DTOs;
using Events.Domain.Entities;
using Events.Domain.Enums;
using Events.Tests.Infrastructure;

namespace Events.Tests.DTOs;

public class EventDtoTests
{
    [Fact]
    public void EventResponseDto_FromEntity_ShouldNotContainVersionOrPriceVersion()
    {
        // Arrange
        var @event = TestEventFactory.Create(price: 1500m, currency: "KZT");

        // Act
        var dto = EventResponseDto.FromEntity(@event);

        // Assert
        dto.Id.Should().Be(@event.Id);
        dto.Title.Should().Be(@event.Title);
        dto.Price.Should().Be(1500m);
        dto.Currency.Should().Be("KZT");
        dto.IsActive.Should().BeTrue();
        dto.AvailableSeats.Should().Be(@event.AvailableSeats);
        dto.TotalSeats.Should().Be(@event.TotalSeats);
    }

    [Fact]
    public void EventCreateDto_DefaultCurrency_ShouldBeKZT()
    {
        // Act
        var dto = new EventCreateDto(
            Title: "Test",
            StartAt: DateTime.UtcNow.AddDays(1),
            EndAt: DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats: 50
        );

        // Assert
        dto.Currency.Should().Be("KZT");
        dto.Price.Should().Be(0.00m);
    }

    [Fact]
    public void EventCreateDto_WithNegativePrice_ValidationShouldFail()
    {
        // Arrange
        var dto = new EventCreateDto(
            Title: "Test",
            StartAt: DateTime.UtcNow.AddDays(1),
            EndAt: DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats: 50,
            Price: -50m
        );
        var validationContext = new ValidationContext(dto);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.ErrorMessage != null && r.ErrorMessage.Contains("Цена не может быть отрицательной"));
    }

    [Fact]
    public void Event_Create_DefaultCurrency_ShouldBeKZT()
    {
        // Act
        var @event = Event.Create(
            title: "Default Currency Event",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(1).AddHours(2),
            totalSeats: 20
        );

        // Assert
        @event.Currency.Should().Be("KZT");
    }

    [Fact]
    public void SeatReservationResponseDto_FromEntity_ShouldMapCorrectly()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        var reservation = SeatReservation.CreateReserved(
            bookingId: bookingId,
            eventId: eventId,
            seats: 3,
            unitPrice: 500m,
            currency: "KZT",
            discountAmount: 0m,
            priceVersion: 1L,
            expiresAt: expiresAt,
            correlationId: Guid.NewGuid(),
            causationId: Guid.NewGuid()
        );

        // Act
        var dto = SeatReservationResponseDto.FromEntity(reservation);

        // Assert
        dto.BookingId.Should().Be(bookingId);
        dto.EventId.Should().Be(eventId);
        dto.Seats.Should().Be(3);
        dto.UnitPrice.Should().Be(500m);
        dto.TotalPrice.Should().Be(1500m);
        dto.Currency.Should().Be("KZT");
        dto.Status.Should().Be(SeatReservationStatus.Reserved);
        dto.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void EventFilter_ShouldHoldPriceAndCurrencyParameters()
    {
        // Act
        var filter = new EventFilter
        {
            MinPrice = 100m,
            MaxPrice = 500m,
            Currency = "KZT"
        };

        // Assert
        filter.MinPrice.Should().Be(100m);
        filter.MaxPrice.Should().Be(500m);
        filter.Currency.Should().Be("KZT");
    }
}
