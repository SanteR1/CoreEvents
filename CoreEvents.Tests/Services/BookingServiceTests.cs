using CoreEvents.Data.DataAccess;
using CoreEvents.Middleware;
using CoreEvents.Models.Domain;
using CoreEvents.Models.DTOs;
using CoreEvents.Services.Implementations;
using CoreEvents.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreEvents.Tests.Services
{
    public class BookingServiceTests
    {
        private readonly IBookingService _bookingService;
        private readonly IServiceScope _scope;
        private readonly IEventService _eventService;
        private readonly ServiceProvider _serviceProvider;

        public BookingServiceTests()
        {
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();

            _serviceProvider = services.BuildServiceProvider();
            _scope = _serviceProvider.CreateScope();
            _eventService = _scope.ServiceProvider.GetRequiredService<IEventService>();
            _bookingService = _scope.ServiceProvider.GetRequiredService<IBookingService>();
        }

        [Fact]
        public async Task CreateBookingAsync_WithValidEvent_ShouldReturnCreatedBookingWithPendingStatus()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var eventEntity = await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Title",
                Description: "Desc",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10));
            BookingCreateDto createDto = new BookingCreateDto(eventEntity.Id);

            // Act
            var result = await _bookingService.CreateBookingAsync(createDto);

            // Assert
            Assert.Equal(BookingStatus.Pending, result.Status);
            Assert.Equal(eventEntity.Id, result.EventId);
        }

        [Fact]
        public async Task CreateBookingAsync_MultipleBookingsForSameEvent_ShouldAssignUniqueIds()
        {
            // Arrange
            const int initialSeats = 10;
            var futureDate = DateTime.UtcNow.AddDays(1);
            var eventEntity = await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Title",
                Description: "Desc",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: initialSeats));

            BookingCreateDto createDto = new BookingCreateDto(eventEntity.Id);

            // Act
            var results = await Task.WhenAll(
                Enumerable.Range(0, initialSeats)
                    .Select(_ => Task.Run(async () =>
                    {

                        using var scope = _serviceProvider.CreateScope();
                        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                        return await bookingService.CreateBookingAsync(createDto);
                    })));

            // Assert
            var uniqueIdsCount = results.Select(r => r.Id).Distinct().Count();
            Assert.Equal(initialSeats, uniqueIdsCount);

            eventEntity = await _eventService.GetEventByIdAsync(eventEntity.Id);
            Assert.All(results, b =>
                Assert.Equal(eventEntity.Id, b.EventId));

            Assert.Equal(0, eventEntity.AvailableSeats);
        }

        [Fact]
        public async Task CreateBookingAsync_MultipleBookingsForSameEvent_ShouldAssignStatusIsPending()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var eventEntity = await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Title",
                Description: "Desc",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10));
            BookingCreateDto createDto = new BookingCreateDto(eventEntity.Id);
            int count = 10;

            // Act
            var results = await Task.WhenAll(
                Enumerable.Range(0, count)
                    .Select(_ => Task.Run(async () =>
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                            return await bookingService.CreateBookingAsync(createDto);
                        }
                    )));

            // Assert
            Assert.All(results, r => Assert.Equal(BookingStatus.Pending, r.Status));
        }

        [Fact]
        public async Task GetBookingByIdAsync_WithValidBookingId_ShouldRetrieveSuccessfully()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var eventEntity = await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Title",
                Description: "Desc",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10));
            var booking = await _bookingService.CreateBookingAsync(new BookingCreateDto(eventEntity.Id));

            // Act
            var result = await _bookingService.GetBookingByIdAsync(booking.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(booking.Id, result.Id);
            Assert.Equal(booking.Status, result.Status);
        }
        
        [Fact]
        public async Task CreateBookingAsync_NonExistingEventId_ShouldThrowNotFoundException()
        {
            // Arrange
            BookingCreateDto createDto = new BookingCreateDto(Guid.NewGuid());
            string expectedExceptionMessage = $"Событие с ID {createDto.EventId} не найдено.";

            // Act
            var exception = await Assert.ThrowsAsync<NotFoundException>(async () =>
                await _bookingService.CreateBookingAsync(createDto)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenEventWasDeleted_ShouldThrowNotFoundException()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(1);
            var eventEntity = await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "To Be Deleted",
                Description: "Desc",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: 10));
            await _bookingService.CreateBookingAsync(new BookingCreateDto(eventEntity.Id));

            string expectedExceptionMessage = $"Событие с ID {eventEntity.Id} не найдено.";
            BookingCreateDto createDto = new BookingCreateDto(eventEntity.Id);

            // Act
            await _eventService.DeleteEventAsync(eventEntity.Id);

            var exception = await Assert.ThrowsAsync<NotFoundException>(async () =>
                await _bookingService.CreateBookingAsync(createDto)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
        }

        [Fact]
        public async Task GetBookingByIdAsync_NonExistingBookingId_ShouldThrowNotFoundException()
        {
            // Arrange
            var nonExistBooking = Guid.NewGuid();
            string expectedExceptionMessage = $"Бронь с ID {nonExistBooking} не найдена.";

            // Act
            var exception = await Assert.ThrowsAsync<NotFoundException>(async () =>
                await _bookingService.GetBookingByIdAsync(nonExistBooking)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenCancellationRequested_ShouldThrowOperationCanceledException()
        {
            // Arrange

            BookingCreateDto createDto = new BookingCreateDto(Guid.NewGuid());
            string expectedExceptionMessage = $"The operation was canceled.";
            var cancellationToken = new CancellationTokenSource();
            cancellationToken.Cancel();

            // Act
            var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _bookingService.CreateBookingAsync(createDto, cancellationToken.Token)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            Assert.Equal(cancellationToken.Token, exception.CancellationToken);
        }

        [Fact]
        public async Task GetBookingByIdAsync_NonExistingBookingId_ShouldThrowOperationCanceledException()
        {
            // Arrange
            string expectedExceptionMessage = $"The operation was canceled.";
            var cancellationToken = new CancellationTokenSource();
            await cancellationToken.CancelAsync();

            // Act
            var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _bookingService.GetBookingByIdAsync(Guid.NewGuid(), cancellationToken.Token)
            );

            // Assert
            Assert.Equal(expectedExceptionMessage, exception.Message);
            Assert.Equal(cancellationToken.Token, exception.CancellationToken);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenBookingCreated_ShouldDecreaseAvailableSeats()
        {
            // Arrange
            const int initialSeats = 3;
            var futureDate = DateTime.UtcNow.AddDays(1);
            var eventEntity = await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Test Event",
                Description: "Desc",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: initialSeats));

            BookingCreateDto createDto = new BookingCreateDto(eventEntity.Id);
            const int expectedSeats = initialSeats - 1;

            // Act
            await _bookingService.CreateBookingAsync(createDto);

            // Assert
            eventEntity = await _eventService.GetEventByIdAsync(eventEntity.Id);
            Assert.Equal(expectedSeats, eventEntity.AvailableSeats);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenSeatsAreDepleted_ShouldAllowSuccessfulBookingsUntilEmpty()
        {
            // Arrange
            const int initialSeats = 2;
            var futureDate = DateTime.UtcNow.AddDays(1);
            var eventEntity = await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Test Event",
                Description: "Desc",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: initialSeats));
            var createDto = new BookingCreateDto(eventEntity.Id);

            // Act & Assert
            await _bookingService.CreateBookingAsync(createDto);
            eventEntity = await _eventService.GetEventByIdAsync(eventEntity.Id);
            Assert.Equal(initialSeats - 1, eventEntity.AvailableSeats);

            // Act & Assert
            await _bookingService.CreateBookingAsync(createDto);
            eventEntity = await _eventService.GetEventByIdAsync(eventEntity.Id);
            Assert.Equal(0, eventEntity.AvailableSeats);

            // Act & Assert
            await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
                _bookingService.CreateBookingAsync(createDto));
        }

        [Fact]
        public async Task CreateBookingAsync_WhenNoSeatsAvailable_ShouldThrowNoAvailableSeatsException()
        {
            // Arrange
            const int initialSeats = 1;
            const string expectedMessage = "No available seats for this event.";
            var futureDate = DateTime.UtcNow.AddDays(1);
            var eventEntity = await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Test Event",
                Description: "Desc",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: initialSeats));
            var createDto = new BookingCreateDto(eventEntity.Id);
            await _bookingService.CreateBookingAsync(createDto);

            // Assert
            eventEntity = await _eventService.GetEventByIdAsync(eventEntity.Id);
            Assert.Equal(0, eventEntity.AvailableSeats);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
                _bookingService.CreateBookingAsync(createDto));

            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenMultipleConcurrentRequests_ShouldPreventOverbooking()
        {
            // Arrange
            const int initialSeats = 5;
            const int totalRequests = 20;
            const int expectedSuccesses = 5;

            var futureDate = DateTime.UtcNow.AddDays(1);
            var eventEntity = await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Test Event",
                Description: "Desc",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: initialSeats));
            BookingCreateDto createDto = new BookingCreateDto(eventEntity.Id);


            var tasks = Enumerable.Range(0, totalRequests)
                .Select(_ => Task.Run(async () =>
                {
                    using var scope = _serviceProvider.CreateScope();
                    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                    return await bookingService.CreateBookingAsync(createDto);
                }))
                .ToArray();

            var allTasks = Task.WhenAll(tasks);

            try
            {
                await allTasks;
            }
            catch { }

            // Assert
            int successCount = tasks.Count(t => t.Status == TaskStatus.RanToCompletion);
            var exceptions = allTasks.Exception?.InnerExceptions ?? Enumerable.Empty<Exception>();

            int noSeatsExceptionCount = exceptions.Count(e => e is NoAvailableSeatsException);
            int otherErrorCount = exceptions.Count(e => e is not NoAvailableSeatsException);


            Assert.Equal(expectedSuccesses, successCount);
            Assert.Equal(totalRequests - expectedSuccesses, noSeatsExceptionCount);
            Assert.Equal(0, otherErrorCount);

            eventEntity = await _eventService.GetEventByIdAsync(eventEntity.Id);
            Assert.Equal(0, eventEntity.AvailableSeats);
        }

        [Fact]
        public async Task CreateBookingAsync_WhenMultipleConcurrentRequests_ShouldAssignUniqueIds()
        {
            // Arrange
            const int initialSeats = 10;
            const int totalRequests = 10;
            const int expectedSuccesses = 10;


            var futureDate = DateTime.UtcNow.AddDays(1);
            var eventEntity = await _eventService.CreateEventAsync(new EventCreateDto(
                Title: "Test Event",
                Description: "Desc",
                StartAt: futureDate,
                EndAt: futureDate.AddHours(2),
                TotalSeats: initialSeats));
            BookingCreateDto createDto = new BookingCreateDto(eventEntity.Id);

            // Act
            var results = await Task.WhenAll(
                Enumerable.Range(0, totalRequests)
                    .Select(_ => Task.Run(async () =>
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                        return await bookingService.CreateBookingAsync(createDto);
                    })));

            // Assert
            var uniqueIdsCount = results.Select(r => r.Id).Distinct().Count();
            Assert.Equal(expectedSuccesses, uniqueIdsCount);

            eventEntity = await _eventService.GetEventByIdAsync(eventEntity.Id);
            Assert.All(results, b =>
                Assert.Equal(eventEntity.Id, b.EventId));

            Assert.Equal(0, eventEntity.AvailableSeats);
        }
    }
}
