using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using Bookings.Application.DTOs;
using Bookings.Domain.Entities;
using Bookings.Domain.Enums;
using CoreEvents.Shared.Contracts.Events;

namespace Bookings.Tests.DTOs;

public class BookingDtoTests
{
    [Fact]
    public void CreateBookingRequestDto_DefaultSeats_ShouldBeOne()
    {
        // Act
        var dto = new CreateBookingRequestDto();

        // Assert
        dto.Seats.Should().Be(1);
    }

    [Fact]
    public void CreateBookingRequestDto_WithCustomSeats_ShouldStoreCorrectValue()
    {
        // Act
        var dto = new CreateBookingRequestDto(Seats: 5);

        // Assert
        dto.Seats.Should().Be(5);
    }

    [Fact]
    public void CreateBookingRequestDto_WhenSeatsLessThanOne_ValidationShouldFail()
    {
        // Arrange
        var dto = new CreateBookingRequestDto(Seats: 0);
        var validationContext = new ValidationContext(dto);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.ErrorMessage != null && r.ErrorMessage.Contains("Количество мест"));
    }

    [Fact]
    public void CancelBookingRequestDto_DefaultReason_ShouldBeUserCancelled()
    {
        // Act
        var dto = new CancelBookingRequestDto();

        // Assert
        dto.Reason.Should().Be(CancellationReason.UserCancelled);
    }

    [Fact]
    public void BookingResponseDto_FromEntity_ShouldContainCorrectValues()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var booking = Booking.Create(eventId, userId, seats: 2);
        booking.Confirm(unitPrice: 500m, totalPrice: 1000m, currency: "KZT", discountAmount: 0m);

        // Act
        var dto = BookingResponseDto.FromEntity(booking);

        // Assert
        dto.Id.Should().Be(booking.Id);
        dto.EventId.Should().Be(eventId);
        dto.UserId.Should().Be(userId);
        dto.Seats.Should().Be(2);
        dto.Status.Should().Be(BookingStatus.Confirmed);
        dto.UnitPrice.Should().Be(500m);
        dto.TotalPrice.Should().Be(1000m);
        dto.Currency.Should().Be("KZT");
        dto.DiscountAmount.Should().Be(0m);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(10, 10)]
    public void PagedFilter_Page_ShouldBeAtLeastOne(int inputPage, int expectedPage)
    {
        // Act
        var filter = new PagedFilter { Page = inputPage };

        // Assert
        filter.Page.Should().Be(expectedPage);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(101, 100)]
    [InlineData(25, 25)]
    public void PagedFilter_PageSize_ShouldBeClampedBetweenOneAndHundred(int inputSize, int expectedSize)
    {
        // Act
        var filter = new PagedFilter { PageSize = inputSize };

        // Assert
        filter.PageSize.Should().Be(expectedSize);
    }
}
