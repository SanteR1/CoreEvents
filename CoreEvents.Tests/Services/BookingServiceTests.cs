using CoreEvents.Application.Configuration;
using CoreEvents.Application.DTOs;
using CoreEvents.Application.Interfaces;
using CoreEvents.Application.Interfaces.Locks;
using CoreEvents.Application.Interfaces.Repositories;
using CoreEvents.Application.Services;
using CoreEvents.Domain.Entities;
using CoreEvents.Domain.Enums;
using CoreEvents.Domain.Exceptions;
using CoreEvents.Tests.Infrastructure;
using FluentAssertions;
using Moq;

namespace CoreEvents.Tests.Services
{
    public class BookingServiceTests
    {
        private readonly Mock<IBookingRepository> _bookingRepositoryMock;
        private readonly Mock<IEventRepository> _eventRepositoryMock;
        private readonly BookingService _bookingService;
        private readonly Mock<ILockProvider> _lockProviderMock;
        private readonly Mock<ILockScope> _lockScopeMock;
        private readonly Mock<IUserContext> _userContextMock;
        private readonly BookingSettings _bookingSettings;
        public BookingServiceTests()
        {
            _bookingRepositoryMock = new Mock<IBookingRepository>();
            _eventRepositoryMock = new Mock<IEventRepository>();

            _lockProviderMock = new Mock<ILockProvider>();

            _lockScopeMock = new Mock<ILockScope>();
            _lockScopeMock.Setup(s => s.CompleteAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _lockScopeMock.Setup(s => s.DisposeAsync())
                .Returns(ValueTask.CompletedTask);

            _lockProviderMock.Setup(p => p.AcquireLockAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_lockScopeMock.Object);

            _userContextMock = new Mock<IUserContext>();

            _bookingSettings = new BookingSettings { MaxBookingsPerUser = 10 };

            _bookingService = new BookingService(_bookingRepositoryMock.Object, _eventRepositoryMock.Object, _lockProviderMock.Object, _userContextMock.Object, _bookingSettings);
        }

        #region CreateBookingAsync
        [Fact]
        public async Task CreateBookingAsync_WithValidEvent_ShouldReturnCreatedBookingWithPendingStatus()
        {
            // Arrange
            var existEvent = TestEventFactory.Create();

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existEvent);

            _userContextMock
                .Setup(repo => repo.UserId)
                .Returns(Guid.NewGuid());

            // Act
            var result = await _bookingService.CreateBookingAsync(new BookingCreateDto(existEvent.Id), TestContext.Current.CancellationToken);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(BookingStatus.Pending);
            result.EventId.Should().Be(existEvent.Id);
            result.ProcessedAt.Should().BeNull();
            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.Add(It.IsAny<Booking>()), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateBookingAsync_NonExistingEventId_ShouldThrowNotFoundException()
        {
            // Arrange
            BookingCreateDto createDto = new BookingCreateDto(Guid.NewGuid());

            // Setup
            _eventRepositoryMock
                .Setup(repo => repo.GetByIdAsync(createDto.EventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Event?)null);

            _userContextMock
                .Setup(repo => repo.UserId)
                .Returns(Guid.NewGuid());

            // Act & Assert
            Func<Task> act = async () => await _bookingService.CreateBookingAsync(createDto);
            var exceptionAssertion = await act.Should().ThrowAsync<DomainNotFoundException>();

            exceptionAssertion.Which.ErrorCode.Should().Be("Event.NotFound");
            exceptionAssertion.Which.ParamName.Should().Be("EventId");
            exceptionAssertion.Which.Key.Should().Be(createDto.EventId.ToString());

            exceptionAssertion.Which.Message.Should().Contain(createDto.EventId.ToString());

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(createDto.EventId, It.IsAny<CancellationToken>()), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.Add(It.IsAny<Booking>()), Times.Never);
            _bookingRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
        {
            // Arrange
            BookingCreateDto createDto = new BookingCreateDto(Guid.NewGuid());
            string expectedExceptionMessage = $"The operation was canceled.";
            var cancellationToken = new CancellationTokenSource();
            await cancellationToken.CancelAsync();

            // Act & Assert
            Func<Task> act = async () => await _bookingService.CreateBookingAsync(createDto, cancellationToken.Token);
            var exceptionAssertion = await act.Should().
                ThrowAsync<OperationCanceledException>().WithMessage(expectedExceptionMessage);
            exceptionAssertion.Which.CancellationToken.Should().Be(cancellationToken.Token);


            _eventRepositoryMock.Verify(
                repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "Сервис не должен обращаться к БД, если запрос был отменен.");

            _bookingRepositoryMock.Verify(
                repo => repo.Add(It.IsAny<Booking>()),
                Times.Never,
                "Сервис не должен создавать бронирование при отмене.");

            _bookingRepositoryMock.Verify(
                repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenValid_ShouldPassTokenToRepository()
        {
            // Arrange
            var existEvent = TestEventFactory.Create();
            var bookingDto = new BookingCreateDto(existEvent.Id);
            using var cts = new CancellationTokenSource();

            // Setup
            _eventRepositoryMock
                .Setup(r => r.GetByIdAsync(bookingDto.EventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existEvent);

            _userContextMock
                .Setup(repo => repo.UserId)
                .Returns(Guid.NewGuid());

            // Act
            await _bookingService.CreateBookingAsync(bookingDto, cts.Token);

            // Assert
            _eventRepositoryMock.Verify(
                repo => repo.GetByIdAsync(bookingDto.EventId, It.IsAny<CancellationToken>()),
                Times.Once,
                "Сервис забыл прокинуть CancellationToken в репозиторий!");
        }

        [Fact]
        public async Task CreateBookingAsync_WhenBookingCreated_ShouldDecreaseAvailableSeats()
        {
            // Arrange
            const int initialSeats = 3;
            var existEvent = TestEventFactory.Create(seats: initialSeats);

            BookingCreateDto createDto = new BookingCreateDto(existEvent.Id);
            const int expectedSeats = initialSeats - 1;

            // Setup
            _eventRepositoryMock
                .Setup(r => r.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existEvent);

            _userContextMock
                .Setup(repo => repo.UserId)
                .Returns(Guid.NewGuid());

            // Act
            var result = await _bookingService.CreateBookingAsync(createDto, TestContext.Current.CancellationToken);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(BookingStatus.Pending);
            existEvent.AvailableSeats.Should().Be(expectedSeats);
            existEvent.TotalSeats.Should().Be(initialSeats);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(createDto.EventId, It.IsAny<CancellationToken>()), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.Add(It.IsAny<Booking>()), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenSeatsAreDepleted_ShouldAllowSuccessfulBookingsUntilEmpty()
        {
            // Arrange
            const int initialSeats = 2;
            var existEvent = TestEventFactory.Create(seats: initialSeats);
            var createDto = new BookingCreateDto(existEvent.Id);

            // Setup
            _eventRepositoryMock.Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existEvent);

            _userContextMock
                .Setup(repo => repo.UserId)
                .Returns(Guid.NewGuid());

            // Act & Assert
            await _bookingService.CreateBookingAsync(createDto, TestContext.Current.CancellationToken);
            existEvent.AvailableSeats.Should().Be(initialSeats - 1);

            await _bookingService.CreateBookingAsync(createDto, TestContext.Current.CancellationToken);
            existEvent.AvailableSeats.Should().Be(0);

            Func<Task> act = async () => await _bookingService.CreateBookingAsync(createDto);
            var exceptionAssertion = await act.Should().ThrowAsync<DomainNoAvailableSeatsException>();

            exceptionAssertion.Which.ErrorCode.Should().Be("Event.NoAvailableSeats");
            exceptionAssertion.Which.EventId.Should().Be(createDto.EventId);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(createDto.EventId, It.IsAny<CancellationToken>()), Times.Exactly(3));
            _bookingRepositoryMock.Verify(repo => repo.Add(It.IsAny<Booking>()), Times.Exactly(2));
            _bookingRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task CreateBookingAsync_WhenNoSeatsAvailable_ShouldThrowNoAvailableSeatsException()
        {
            // Arrange
            const int initialSeats = 1;
            var existEvent = TestEventFactory.Create(seats: initialSeats);
            var createDto = new BookingCreateDto(existEvent.Id);

            // Setup
            _eventRepositoryMock.Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existEvent);

            _userContextMock
                .Setup(repo => repo.UserId)
                .Returns(Guid.NewGuid());

            // Act & Assert
            await _bookingService.CreateBookingAsync(createDto, TestContext.Current.CancellationToken);
            existEvent.Should().NotBeNull();
            existEvent.AvailableSeats.Should().Be(0);

            // Act & Assert
            Func<Task> act = async () => await _bookingService.CreateBookingAsync(createDto);
            var exceptionAssertion = await act.Should().ThrowAsync<DomainNoAvailableSeatsException>();

            exceptionAssertion.Which.ErrorCode.Should().Be("Event.NoAvailableSeats");
            exceptionAssertion.Which.EventId.Should().Be(createDto.EventId);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(createDto.EventId, It.IsAny<CancellationToken>()), Times.Exactly(2));
            _bookingRepositoryMock.Verify(repo => repo.Add(It.IsAny<Booking>()), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenEventPastOrStart_ShouldThrowDomainPastEventBookingException()
        {
            // Arrange
            const int initialSeats = 1;
            var existEvent = TestEventFactory.CreatePast(
                36,
                "Past Event",
                "Past description",
                seats: initialSeats);
            var createDto = new BookingCreateDto(existEvent.Id);

            // Setup
            _eventRepositoryMock.Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existEvent);

            _userContextMock
                .Setup(repo => repo.UserId)
                .Returns(Guid.NewGuid());

            // Act & Assert
            Func<Task> act = async () => await _bookingService.CreateBookingAsync(createDto, TestContext.Current.CancellationToken);
            var exceptionAssertion = await act.Should().ThrowAsync<DomainPastEventBookingException>();

            exceptionAssertion.Which.ErrorCode.Should().Be("Event.PastEventBooking");
            exceptionAssertion.Which.EventId.Should().Be(createDto.EventId);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(createDto.EventId, It.IsAny<CancellationToken>()), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.Add(It.IsAny<Booking>()), Times.Never);
            _bookingRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenExceedingLimitActiveBooking_ShouldThrowDomainActiveBookingLimitExceededException()
        {
            // Arrange
            const int initialSeats = 1;
            const int userCountBooking = 10;
            var existEvent = TestEventFactory.Create(seats: initialSeats);
            var createDto = new BookingCreateDto(existEvent.Id);

            // Setup
            var userId = Guid.NewGuid();
            _eventRepositoryMock.Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existEvent);

            _userContextMock
                .Setup(repo => repo.UserId)
                .Returns(userId);

            _bookingRepositoryMock.Setup(repo => repo.GetBookingCountForUserAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userCountBooking);

            // Act & Assert
            Func<Task> act = async () => await _bookingService.CreateBookingAsync(createDto);
            var exceptionAssertion = await act.Should().ThrowAsync<DomainActiveBookingLimitExceededException>();

            exceptionAssertion.Which.ErrorCode.Should().Be("Booking.LimitBooking");
            exceptionAssertion.Which.Max.Should().Be(userCountBooking);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(createDto.EventId, It.IsAny<CancellationToken>()), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.Add(It.IsAny<Booking>()), Times.Never);
            _bookingRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateBookingAsync_ForDifferentUsers_ShouldIsolateActiveBookingLimits()
        {
            // Arrange
            const int initialSeats = 10;
            const int userOneCountBooking = 10;
            const int userTwoCountBooking = 5;
            var existEvent = TestEventFactory.Create(seats: initialSeats);
            var createDto = new BookingCreateDto(existEvent.Id);
            var userOne = Guid.NewGuid();
            var userTwo = Guid.NewGuid();
            var currentUser = userOne;
            // Setup

            _eventRepositoryMock.Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existEvent);

            _userContextMock
                .Setup(repo => repo.UserId)
                .Returns(() => currentUser);

            _bookingRepositoryMock.Setup(repo => repo.GetBookingCountForUserAsync(userOne, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userOneCountBooking);
            _bookingRepositoryMock.Setup(repo => repo.GetBookingCountForUserAsync(userTwo, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userTwoCountBooking);

            // Act & Assert
            Func<Task> act = async () => await _bookingService.CreateBookingAsync(createDto);
            var exceptionAssertion = await act.Should().ThrowAsync<DomainActiveBookingLimitExceededException>();

            exceptionAssertion.Which.ErrorCode.Should().Be("Booking.LimitBooking");
            exceptionAssertion.Which.Max.Should().Be(userOneCountBooking);

            currentUser = userTwo;
            var userTwoBooking = await _bookingService.CreateBookingAsync(createDto, TestContext.Current.CancellationToken);
            userTwoBooking.Should().NotBeNull();
            userTwoBooking.Status.Should().Be(BookingStatus.Pending);
            existEvent.AvailableSeats.Should().Be(9);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(createDto.EventId, It.IsAny<CancellationToken>()), Times.Exactly(2));
            _bookingRepositoryMock.Verify(repo => repo.Add(It.IsAny<Booking>()), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenUnauthorizedAccess_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            const int initialSeats = 1;
            var existEvent = TestEventFactory.Create(seats: initialSeats);
            var createDto = new BookingCreateDto(existEvent.Id);

            // Setup
            _eventRepositoryMock.Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existEvent);

            // Act & Assert
            Func<Task> act = async () => await _bookingService.CreateBookingAsync(createDto);
            var exceptionAssertion = await act.Should().ThrowAsync<DomainUnauthorizedAccessException>();

            exceptionAssertion.Which.ErrorCode.Should().Be("Authorization.Denied");

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(createDto.EventId, It.IsAny<CancellationToken>()), Times.Never);
            _bookingRepositoryMock.Verify(repo => repo.Add(It.IsAny<Booking>()), Times.Never);
            _bookingRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
        #endregion

        #region CancelBookingByIdAsync
        [Fact]
        public async Task CancelBookingByIdAsync_WhenUserNotOwnersForBooking_ShouldThrowDomainNotBookingOwnerException()
        {
            // Arrange
            const int initialSeats = 1;
            var userOwnerId = Guid.NewGuid();
            var userNotOwnerId = Guid.NewGuid();
            var existEvent = TestEventFactory.Create(seats: initialSeats);
            var createDto = new BookingCreateDto(existEvent.Id);

            // Setup
            _eventRepositoryMock.Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existEvent);

            _userContextMock
                .SetupSequence(repo => repo.UserId)
                .Returns(userOwnerId)
                .Returns(userNotOwnerId);

            // Act & Assert
            var booking = await _bookingService.CreateBookingAsync(createDto, TestContext.Current.CancellationToken);

            var domainBooking = Booking.Create(existEvent.Id, userOwnerId);
            _bookingRepositoryMock.Setup(repo => repo.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(domainBooking);

            Func<Task> act = async () => await _bookingService.CancelBookingByIdAsync(booking.Id);
            var exceptionAssertion = await act.Should().ThrowAsync<DomainNotBookingOwnerException>();

            exceptionAssertion.Which.ErrorCode.Should().Be("Access.Denied");
            exceptionAssertion.Which.BookingId.Should().Be(booking.Id);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(createDto.EventId, It.IsAny<CancellationToken>()), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.Add(It.IsAny<Booking>()), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CancelBookingAsync_WithRoleIsAdmin_ShouldCancelledBooking()
        {
            // Arrange
            const int initialSeats = 10;
            var existEvent = TestEventFactory.Create(seats: initialSeats);
            var createDto = new BookingCreateDto(existEvent.Id);
            var roleUser = Guid.NewGuid();
            var roleAdmin = Guid.NewGuid();
            Guid currentUserId = roleUser;
            RoleName currentRole = RoleName.User;
            // Setup
            _eventRepositoryMock.Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existEvent);

            _userContextMock
                .Setup(repo => repo.UserId)
                .Returns(() => currentUserId);

            _userContextMock
                .Setup(repo => repo.Role)
                .Returns(() => currentRole);

            // Act & Assert
            var booking = await _bookingService.CreateBookingAsync(createDto, TestContext.Current.CancellationToken);

            var domainBooking = Booking.Create(existEvent.Id, roleUser);
            _bookingRepositoryMock.Setup(repo => repo.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(domainBooking);
            existEvent.Should().NotBeNull();
            existEvent.AvailableSeats.Should().Be(9);

            currentUserId = roleAdmin;
            currentRole = RoleName.Admin;
            // Act & Assert
            await _bookingService.CancelBookingByIdAsync(booking.Id, TestContext.Current.CancellationToken);
            existEvent.AvailableSeats.Should().Be(10);
            domainBooking.Status.Should().Be(BookingStatus.Cancelled);

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(createDto.EventId, It.IsAny<CancellationToken>()), Times.Exactly(2));
            _bookingRepositoryMock.Verify(repo => repo.Add(It.IsAny<Booking>()), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
        #endregion

        #region GetBookingByIdAsync
        [Fact]
        public async Task GetBookingByIdAsync_NonExistingBookingId_ShouldThrowNotFoundException()
        {
            // Arrange
            var nonExistBooking = Guid.NewGuid();

            // Setup
            _bookingRepositoryMock
                .Setup(repo => repo.GetByIdAsync(nonExistBooking, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Booking?)null);

            _userContextMock
                .Setup(repo => repo.UserId)
                .Returns(Guid.NewGuid());

            // Act & Assert
            Func<Task> act = async () => await _bookingService.GetBookingByIdAsync(nonExistBooking);
            var exceptionAssertion = await act.Should().ThrowAsync<DomainNotFoundException>();

            exceptionAssertion.Which.ErrorCode.Should().Be("Booking.NotFound");
            exceptionAssertion.Which.ParamName.Should().Be("id"); //
            exceptionAssertion.Which.Key.Should().Be(nonExistBooking.ToString());

            exceptionAssertion.Which.Message.Should().Contain(nonExistBooking.ToString());

            _bookingRepositoryMock.Verify(repo => repo.GetByIdAsync(nonExistBooking, It.IsAny<CancellationToken>()), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.Add(It.IsAny<Booking>()), Times.Never);
            _bookingRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
        [Fact]
        public async Task GetBookingByIdAsync_WithValidBookingId_ShouldRetrieveSuccessfully()
        {
            // Arrange
            var existEvent = TestEventFactory.Create();

            // Setup
            _eventRepositoryMock.Setup(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existEvent);

            _userContextMock
                .Setup(repo => repo.UserId)
                .Returns(Guid.NewGuid());
            _userContextMock
                .Setup(repo => repo.Role)
                .Returns(RoleName.Admin);

            // Act
            var booking = await _bookingService.CreateBookingAsync(new BookingCreateDto(existEvent.Id), It.IsAny<CancellationToken>());

            // Assert
            booking.Should().NotBeNull();
            booking.EventId.Should().Be(existEvent.Id);
            booking.Status.Should().Be(BookingStatus.Pending);
            booking.CreatedAt.Should().NotBe(default);
            booking.ProcessedAt.Should().BeNull();

            _eventRepositoryMock.Verify(repo => repo.GetByIdAsync(existEvent.Id, It.IsAny<CancellationToken>()), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.Add(It.IsAny<Booking>()), Times.Once);
            _bookingRepositoryMock.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetBookingByIdAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
        {
            // Arrange
            string expectedExceptionMessage = $"The operation was canceled.";
            var cancellationToken = new CancellationTokenSource();
            await cancellationToken.CancelAsync();

            // Act & Assert
            Func<Task> act = async () =>
                await _bookingService.GetBookingByIdAsync(Guid.NewGuid(), cancellationToken.Token);
            var exceptionAssertion = await act.Should().ThrowAsync<OperationCanceledException>()
                .WithMessage(expectedExceptionMessage);
            exceptionAssertion.Which.CancellationToken.Should().Be(cancellationToken.Token);

            _eventRepositoryMock.Verify(
                repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "Сервис не должен обращаться к БД, если запрос был отменен.");

            _bookingRepositoryMock.Verify(
                repo => repo.Add(It.IsAny<Booking>()),
                Times.Never,
                "Сервис не должен создавать бронирование при отмене.");

            _bookingRepositoryMock.Verify(
                repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetBookingByIdAsync_WhenValid_ShouldPassTokenToRepository()
        {
            // Arrange
            var existEvent = TestEventFactory.Create();
            var booking = Booking.Create(existEvent.Id, Guid.NewGuid());
            using var cts = new CancellationTokenSource();

            // Setup
            _bookingRepositoryMock
                .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(booking);

            // Act
            await _bookingService.GetBookingByIdAsync(booking.Id, cts.Token);

            // Assert
            _bookingRepositoryMock.Verify(
                repo => repo.GetByIdAsync(booking.Id, cts.Token),
                Times.Once,
                "Сервис забыл прокинуть CancellationToken в репозиторий!");
        }
        #endregion
    }
}
