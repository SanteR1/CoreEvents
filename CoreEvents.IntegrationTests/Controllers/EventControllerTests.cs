using System.Net;
using System.Net.Http.Json;
using CoreEvents.Application.DTOs;
using CoreEvents.Domain.Entities;
using CoreEvents.Domain.Enums;
using CoreEvents.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace CoreEvents.IntegrationTests.Controllers
{
    public class EventControllerTests(IntegrationTestFactory factory) : IntegrationTestBase(factory)
    {
        private readonly HttpClient _client = factory.CreateClient();

        [Fact]
        public async Task CreateEvent_WithValidRequest_ShouldSaveToDbAndReturnCreated()
        {
            // Arrange
            var startAt = DateTime.UtcNow.AddDays(2);
            var endAt = DateTime.UtcNow.AddDays(2).AddHours(2);

            var eventCreateDto = new EventCreateDto(
                Title: "Event Test",
                StartAt: startAt,
                EndAt: endAt,
                TotalSeats: 15,
                Description: "Test Description"
                );

            // Act
            var response = await _client.PostAsJsonAsync("/events", eventCreateDto, TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            // Act
            var returnedEvent = await response.Content.ReadFromJsonAsync<EventResponseDto>(TestContext.Current.CancellationToken);

            // Assert
            returnedEvent.Should().NotBeNull();
            returnedEvent.Id.Should().NotBeEmpty();
            returnedEvent.Title.Should().Be("Event Test");
            returnedEvent.TotalSeats.Should().Be(15);
            returnedEvent.AvailableSeats.Should().Be(15);
            returnedEvent.Description.Should().Be("Test Description");
            returnedEvent.StartAt.Should().BeCloseTo(startAt, TimeSpan.FromMilliseconds(1));
            returnedEvent.EndAt.Should().BeCloseTo(endAt, TimeSpan.FromMilliseconds(1));

            await ExecuteDbContextAsync(async db =>
            {
                var eventInDb = await db.Events.FindAsync(returnedEvent.Id);
                eventInDb.Should().NotBeNull();
                eventInDb.Title.Should().Be(eventCreateDto.Title);
                eventInDb.Description.Should().Be(eventCreateDto.Description);
                eventInDb.AvailableSeats.Should().Be(15);
            });
        }

        [Fact]
        public async Task CreateBooking_WithValidRequest_ShouldSaveToDbAndReturnCreatedWithLocation()
        {
            // Arrange
            var eventCreate = await ExecuteDbContextAsync(async ctx =>
            {
                var futureDate1 = DateTime.UtcNow.AddDays(1);
                var futureDate2 = futureDate1.AddHours(1);

                var eventCreate = Event.Create($"Test Event for Booking", futureDate1, futureDate2, 5);

                await ctx.AddAsync(eventCreate);
                await ctx.SaveChangesAsync();
                return eventCreate;
            });

            // Act
            var response = await _client.PostAsync($"/events/{eventCreate.Id}/book", content: null, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);

            // Act
            var returnedBooking = await response.Content.ReadFromJsonAsync<BookingResponseDto>(DefaultJsonOptions, TestContext.Current.CancellationToken);

            // Assert
            returnedBooking.Should().NotBeNull();
            returnedBooking.Id.Should().NotBe(Guid.Empty);
            returnedBooking.EventId.Should().NotBeEmpty().And.Be(eventCreate.Id);
            response.Headers.Location.Should().NotBeNull();
            response.Headers.Location.ToString().Should().Contain($"{returnedBooking.Id}");
            returnedBooking.Status.Should().Be(BookingStatus.Pending);

            await ExecuteDbContextAsync(async db =>
            {
                var bookingInDb = await db.Bookings.FindAsync(returnedBooking.Id);

                bookingInDb.Should().NotBeNull();
                bookingInDb.Id.Should().Be(returnedBooking.Id);
                bookingInDb.EventId.Should().Be(eventCreate.Id);
                bookingInDb.Status.Should().Be(BookingStatus.Pending);
            });
        }
    }
}
