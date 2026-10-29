using AwesomeAssertions;
using Bookings.Application.Abstractions.Repositories;
using Bookings.Application.DTOs;
using Bookings.Application.Queries;
using Bookings.Domain.Entities;
using Bookings.Domain.Enums;
using CoreEvents.Shared.Contracts.Identity.Enums;
using Moq;

namespace Bookings.Tests.Queries;

public class GetUserBookingsHandlerTests
{
    private readonly Mock<IBookingRepository> _repositoryMock = new();
    private readonly GetUserBookingsHandler _handler;

    public GetUserBookingsHandlerTests()
    {
        _handler = new GetUserBookingsHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_UserRole_ShouldPassIsAdminFalseToRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var filter = new BookingFilter { Page = 1, PageSize = 10 };
        var query = new GetUserBookingsQuery(userId, RoleName.User, filter);

        var booking = Booking.Create(Guid.NewGuid(), userId, 2);
        var pagedResult = new PaginatedResult<Booking>([booking], 1, 1, 10);

        _repositoryMock
            .Setup(r => r.GetUserBookingsAsync(userId, false, filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(booking.Id);
        result.Items[0].UserId.Should().Be(userId);
        result.Items[0].Seats.Should().Be(2);

        _repositoryMock.Verify(r => r.GetUserBookingsAsync(userId, false, filter, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminRole_ShouldPassIsAdminTrueToRepository()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var filter = new BookingFilter { UserId = targetUserId, Status = BookingStatus.Confirmed, Page = 1, PageSize = 10 };
        var query = new GetUserBookingsQuery(adminId, RoleName.Admin, filter);

        var booking = Booking.Create(Guid.NewGuid(), targetUserId, 1);
        booking.Confirm(100m, 100m, "KZT");
        var pagedResult = new PaginatedResult<Booking>([booking], 1, 1, 10);

        _repositoryMock
            .Setup(r => r.GetUserBookingsAsync(adminId, true, filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items[0].Status.Should().Be(BookingStatus.Confirmed);

        _repositoryMock.Verify(r => r.GetUserBookingsAsync(adminId, true, filter, It.IsAny<CancellationToken>()), Times.Once);
    }
}
