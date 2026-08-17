using Bookings.Application.Abstractions.Repositories;
using Bookings.Application.Exceptions;
using Bookings.Application.Queries;
using Bookings.Domain.Entities;
using Bookings.Domain.Exceptions;
using CoreEvents.Shared.Contracts.Identity.Enums;
using FluentAssertions;
using Moq;

namespace Bookings.Tests.Queries;

public class GetBookingByIdHandlerTests
{
    private readonly Mock<IBookingRepository> _repositoryMock;
    private readonly GetBookingByIdHandler _handler;

    public GetBookingByIdHandlerTests()
    {
        _repositoryMock = new Mock<IBookingRepository>();
        _handler = new GetBookingByIdHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ThrowBookingNotFoundException_When_BookingDoesNotExist()
    {
        // Arrange
        var query = new GetBookingByIdQuery(Guid.NewGuid(), Guid.NewGuid(), RoleName.User);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(query.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Booking?)null);

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BookingNotFoundException>()
                 .WithMessage($"*{query.BookingId}*");
    }

    [Fact]
    public async Task Handle_Should_Return_When_UserIsAdmin_EvenIfHeIsNotOwner()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var booking = Booking.Create(eventId, ownerId);
        var query = new GetBookingByIdQuery(booking.Id, adminId, RoleName.Admin);


        _repositoryMock
            .Setup(r => r.GetByIdAsync(query.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.EventId.Should().Be(eventId);
    }

    [Fact]
    public async Task Handle_Should_Return_When_UserIsOwner()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var query = new GetBookingByIdQuery(Guid.NewGuid(), ownerId, RoleName.User);
        var booking = Booking.Create(eventId, ownerId);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(query.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_ThrowException_When_UserIsNotOwner_And_NotAdmin()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var hackerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var query = new GetBookingByIdQuery(Guid.NewGuid(), hackerId, RoleName.User);
        var booking = Booking.Create(eventId, ownerId);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(query.BookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotBookingOwnerException>();
    }
}
