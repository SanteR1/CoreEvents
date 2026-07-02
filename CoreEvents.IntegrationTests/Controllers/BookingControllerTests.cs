using System.Net;
using System.Net.Http.Json;
using CoreEvents.Application.DTOs;
using CoreEvents.Domain.Entities;
using CoreEvents.Domain.Enums;
using CoreEvents.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace CoreEvents.IntegrationTests.Controllers
{
    public class BookingControllerTests(IntegrationTestFactory factory):IntegrationTestBase(factory)
    {
        private readonly HttpClient _client = factory.CreateClient();

        [Fact]
        public  async Task GetBookingStatus_WithValidRequest_ShouldReturnCreateAnd()
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

            // Act & Assert
            var responseCreate = await _client.PostAsync($"/events/{eventCreate.Id}/book", content: null, cancellationToken: TestContext.Current.CancellationToken);


            responseCreate.StatusCode.Should().Be(HttpStatusCode.Accepted);
            var returnedCreate = await responseCreate.Content.ReadFromJsonAsync<BookingResponseDto>(DefaultJsonOptions, TestContext.Current.CancellationToken);

            returnedCreate.Should().NotBeNull();
            returnedCreate.Id.Should().NotBe(Guid.Empty);

            var response = await _client.GetAsync($"/bookings/{returnedCreate.Id}", cancellationToken: TestContext.Current.CancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var returnedBooking = await response.Content.ReadFromJsonAsync<BookingResponseDto>(DefaultJsonOptions, TestContext.Current.CancellationToken);

            returnedBooking.Should().NotBeNull();
            returnedBooking.Id.Should().Be(returnedCreate.Id);
            returnedBooking.Status.Should().Be(BookingStatus.Pending);
            returnedBooking.EventId.Should().Be(eventCreate.Id);
        }
    }
}
