using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CoreEvents.Application.DTOs;
using CoreEvents.Domain.Entities;
using CoreEvents.Domain.Enums;
using CoreEvents.IntegrationTests.Infrastructure.Bases;
using CoreEvents.IntegrationTests.Infrastructure.Factories;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CoreEvents.IntegrationTests.Controllers
{
    public class BookingControllerTests(ApiOnlyIntegrationTestFactory factory) : ApiOnlyIntegrationTestBase(factory)
    {
        private readonly HttpClient _client = factory.CreateClient();

        [Fact]
        public async Task GetBookingStatus_WithValidRequest_ShouldReturnCreateAnd()
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

            var registerResponse = await _client.PostAsJsonAsync(
                "/auth/register",
                new UserRequestDto("testuser", "123", "User"),
                TestContext.Current.CancellationToken);
            registerResponse.EnsureSuccessStatusCode();
            var loginResponse = await _client.PostAsJsonAsync(
                "/auth/login",
                new UserLoginDto("testuser", "123"),
                TestContext.Current.CancellationToken);
            loginResponse.EnsureSuccessStatusCode();

            var authResult = await loginResponse.Content.ReadFromJsonAsync<string>(TestContext.Current.CancellationToken);
            var token = authResult;
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

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

        [Fact]
        public async Task PostBookingCancel_WithNotBookingOwner_ShouldReturnHttpStatusCodeForbidden()
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

            var registerOwnerResponse = await _client.PostAsJsonAsync(
                "/auth/register",
                new UserRequestDto("Owner", "123", "User"),
                TestContext.Current.CancellationToken);
            registerOwnerResponse.EnsureSuccessStatusCode();
            var loginResponse = await _client.PostAsJsonAsync(
                "/auth/login",
                new UserLoginDto("Owner", "123"),
                TestContext.Current.CancellationToken);
            loginResponse.EnsureSuccessStatusCode();

            var authResult = await loginResponse.Content.ReadFromJsonAsync<string>(TestContext.Current.CancellationToken);
            var token = authResult;
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

            // Act & Assert
            var responseCreate = await _client.PostAsync($"/events/{eventCreate.Id}/book", content: null, cancellationToken: TestContext.Current.CancellationToken);

            responseCreate.StatusCode.Should().Be(HttpStatusCode.Accepted);
            var returnedCreate = await responseCreate.Content.ReadFromJsonAsync<BookingResponseDto>(DefaultJsonOptions, TestContext.Current.CancellationToken);

            returnedCreate.Should().NotBeNull();
            returnedCreate.Id.Should().NotBe(Guid.Empty);

            var registerOtherUserResponse = await _client.PostAsJsonAsync(
                "/auth/register",
                new UserRequestDto("Other", "123", "User"),
                TestContext.Current.CancellationToken);
            registerOtherUserResponse.EnsureSuccessStatusCode();
            var loginOtherResponse = await _client.PostAsJsonAsync(
                "/auth/login",
                new UserLoginDto("Other", "123"),
                TestContext.Current.CancellationToken);
            loginOtherResponse.EnsureSuccessStatusCode();

            var authOtherResult = await loginOtherResponse.Content.ReadFromJsonAsync<string>(TestContext.Current.CancellationToken);
            var tokenOther = authOtherResult;
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, tokenOther);

            var response = await _client.DeleteAsync($"/bookings/{returnedCreate.Id}", TestContext.Current.CancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task PostBookingCancel_WithRoleAdminAndNotBookingOwner_ShouldCanceledBookingAndReturnHttpStatusCodeNoContent()
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

            var registerOwnerResponse = await _client.PostAsJsonAsync(
                "/auth/register",
                new UserRequestDto("Owner", "123", "User"),
                TestContext.Current.CancellationToken);
            registerOwnerResponse.EnsureSuccessStatusCode();
            var loginResponse = await _client.PostAsJsonAsync(
                "/auth/login",
                new UserLoginDto("Owner", "123"),
                TestContext.Current.CancellationToken);
            loginResponse.EnsureSuccessStatusCode();

            var authResult = await loginResponse.Content.ReadFromJsonAsync<string>(TestContext.Current.CancellationToken);
            var token = authResult;
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

            // Act & Assert
            var responseCreate = await _client.PostAsync($"/events/{eventCreate.Id}/book", content: null, cancellationToken: TestContext.Current.CancellationToken);

            responseCreate.StatusCode.Should().Be(HttpStatusCode.Accepted);
            var returnedCreate = await responseCreate.Content.ReadFromJsonAsync<BookingResponseDto>(DefaultJsonOptions, TestContext.Current.CancellationToken);

            returnedCreate.Should().NotBeNull();
            returnedCreate.Id.Should().NotBe(Guid.Empty);

            var registerAdminUserResponse = await _client.PostAsJsonAsync(
                "/auth/register",
                new UserRequestDto("Admin", "123", "Admin"),
                TestContext.Current.CancellationToken);
            registerAdminUserResponse.EnsureSuccessStatusCode();
            var loginAdminResponse = await _client.PostAsJsonAsync(
                "/auth/login",
                new UserLoginDto("Admin", "123"),
                TestContext.Current.CancellationToken);
            loginAdminResponse.EnsureSuccessStatusCode();

            var authAdminResult = await loginAdminResponse.Content.ReadFromJsonAsync<string>(TestContext.Current.CancellationToken);
            var tokenAdmin = authAdminResult;
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, tokenAdmin);

            var response = await _client.DeleteAsync($"/bookings/{returnedCreate.Id}", TestContext.Current.CancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            await ExecuteDbContextAsync(async db =>
            {
                var bookingInDb = await db.Bookings.FindAsync(returnedCreate.Id, TestContext.Current.CancellationToken);

                bookingInDb.Should().NotBeNull();
                bookingInDb.Id.Should().NotBeEmpty();
                bookingInDb.Status.Should().Be(BookingStatus.Cancelled);
            });
        }
    }
}
