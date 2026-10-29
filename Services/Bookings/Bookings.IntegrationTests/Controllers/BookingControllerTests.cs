using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;
using Bookings.Application.DTOs;
using Bookings.Domain.Entities;
using Bookings.Domain.Enums;
using Bookings.IntegrationTests.Infrastructure.Auth;
using Bookings.IntegrationTests.Infrastructure.Bases;
using Bookings.IntegrationTests.Infrastructure.Factories;
using CoreEvents.Shared.Contracts.Events;

namespace Bookings.IntegrationTests.Controllers;

public sealed class BookingControllerTests(ApiOnlyIntegrationTestFactory factory) : ApiOnlyIntegrationTestBase(factory)
{
    [Fact]
    public async Task GetBookingStatus_WithValidRequest_ShouldReturnCreateAnd()
    {
        // Arrange
        var eventExist = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await ExecuteDbContextAsync(async ctx =>
        {
            ctx.EventProjections.Add(EventProjection.Create(eventExist, "Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 500m, "KZT", 1L, 100));
            await ctx.SaveChangesAsync();
        });

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "User");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Guid", userId.ToString());

        // Act & Assert
        using var responseCreate = await HttpClient.PostAsync($"/v1/bookings/{eventExist}/book", content: null, cancellationToken: TestContext.Current.CancellationToken);

        responseCreate.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var returnedCreate = await responseCreate.Content.ReadFromJsonAsync<BookingResponseDto>(DefaultJsonOptions, TestContext.Current.CancellationToken);

        returnedCreate.Should().NotBeNull();
        returnedCreate.Id.Should().NotBe(Guid.Empty);

        using var response = await HttpClient.GetAsync($"/v1/bookings/{returnedCreate.Id}", cancellationToken: TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var returnedBooking = await response.Content.ReadFromJsonAsync<BookingResponseDto>(DefaultJsonOptions, TestContext.Current.CancellationToken);

        returnedBooking.Should().NotBeNull();
        returnedBooking.Id.Should().Be(returnedCreate.Id);
        returnedBooking.Status.Should().Be(BookingStatus.Pending);
        returnedBooking.EventId.Should().Be(eventExist);
    }

    [Fact]
    public async Task PostBookingCancel_WithNotBookingOwner_ShouldReturnHttpStatusCodeForbidden()
    {
        // Arrange
        Guid ownerId = Guid.NewGuid();
        Guid hackUserId = Guid.NewGuid();
        Guid eventExist = Guid.NewGuid();

        await ExecuteDbContextAsync(async ctx =>
        {
            ctx.EventProjections.Add(EventProjection.Create(eventExist, "Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 500m, "KZT", 1L, 100));
            await ctx.SaveChangesAsync();
        });

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "User");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Guid", ownerId.ToString());

        // Act & Assert
        using HttpResponseMessage responseCreate = await HttpClient.PostAsync($"/v1/bookings/{eventExist}/book", content: null, cancellationToken: TestContext.Current.CancellationToken);

        responseCreate.StatusCode.Should().Be(HttpStatusCode.Accepted);
        BookingResponseDto? returnedCreate = await responseCreate.Content.ReadFromJsonAsync<BookingResponseDto>(DefaultJsonOptions, TestContext.Current.CancellationToken);

        returnedCreate.Should().NotBeNull();
        returnedCreate.Id.Should().NotBe(Guid.Empty);

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "User");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Guid", hackUserId.ToString());

        using HttpResponseMessage response = await HttpClient.DeleteAsync($"/v1/bookings/{returnedCreate.Id}", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostBookingCancel_WhenBookingIsPending_ShouldReturnConflict()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var eventExist = Guid.NewGuid();

        await ExecuteDbContextAsync(async ctx =>
        {
            ctx.EventProjections.Add(EventProjection.Create(eventExist, "Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 500m, "KZT", 1L, 100));
            await ctx.SaveChangesAsync();
        });

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "User");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Guid", ownerId.ToString());

        using var responseCreate = await HttpClient.PostAsync($"/v1/bookings/{eventExist}/book", content: null, cancellationToken: TestContext.Current.CancellationToken);
        responseCreate.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var returnedCreate = await responseCreate.Content.ReadFromJsonAsync<BookingResponseDto>(DefaultJsonOptions, TestContext.Current.CancellationToken);
        returnedCreate.Should().NotBeNull();

        // Act: cancellation during Pending should be forbidden (409 Conflict)
        using var response = await HttpClient.DeleteAsync($"/v1/bookings/{returnedCreate.Id}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PostBookingCancel_WithRoleAdminAndNotBookingOwner_ShouldRequestCanceledBookingAndReturnHttpStatusCodeAccepted()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var eventExist = Guid.NewGuid();

        await ExecuteDbContextAsync(async ctx =>
        {
            ctx.EventProjections.Add(EventProjection.Create(eventExist, "Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 500m, "KZT", 1L, 100));
            await ctx.SaveChangesAsync();
        });

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "User");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Guid", ownerId.ToString());

        // Act & Assert
        using var responseCreate = await HttpClient.PostAsync($"/v1/bookings/{eventExist}/book", content: null, cancellationToken: TestContext.Current.CancellationToken);

        responseCreate.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var returnedCreate = await responseCreate.Content.ReadFromJsonAsync<BookingResponseDto>(DefaultJsonOptions, TestContext.Current.CancellationToken);

        returnedCreate.Should().NotBeNull();
        returnedCreate.Id.Should().NotBe(Guid.Empty);

        // Transition booking to Confirmed so it can be canceled
        await ExecuteDbContextAsync(async ctx =>
        {
            var b = await ctx.Bookings.FindAsync(returnedCreate.Id);
            b!.Confirm(500m, 500m, "KZT", 0m);
            await ctx.SaveChangesAsync();
        });

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Guid", adminId.ToString());

        using var response = await HttpClient.DeleteAsync($"/v1/bookings/{returnedCreate.Id}", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var returnedCancel = await response.Content.ReadFromJsonAsync<BookingResponseDto>(DefaultJsonOptions, TestContext.Current.CancellationToken);
        returnedCancel.Should().NotBeNull();
        returnedCancel.Status.Should().Be(BookingStatus.CancellationPending);
    }

    [Fact]
    public async Task CreateBooking_WithExplicitSeats_ShouldReturnAcceptedAndHaveRequestedSeats()
    {
        // Arrange
        var eventExist = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await ExecuteDbContextAsync(async ctx =>
        {
            ctx.EventProjections.Add(EventProjection.Create(eventExist, "Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 500m, "KZT", 1L, 100));
            await ctx.SaveChangesAsync();
        });

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "User");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Guid", userId.ToString());

        var requestDto = new CreateBookingRequestDto(Seats: 3);

        // Act
        using var response = await HttpClient.PostAsJsonAsync($"/v1/bookings/{eventExist}/book", requestDto, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var returned = await response.Content.ReadFromJsonAsync<BookingResponseDto>(DefaultJsonOptions, TestContext.Current.CancellationToken);
        returned.Should().NotBeNull();
        returned.Seats.Should().Be(3);
        returned.Currency.Should().Be("KZT");
    }

    [Fact]
    public async Task GetMyBookings_WithPagination_ShouldReturnPaginatedBookings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventExist = Guid.NewGuid();

        await ExecuteDbContextAsync(async ctx =>
        {
            ctx.EventProjections.Add(EventProjection.Create(eventExist, "Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 500m, "KZT", 1L, 100));
            var b1 = Booking.Create(eventExist, userId, 1);
            var b2 = Booking.Create(eventExist, userId, 2);
            ctx.Bookings.AddRange(b1, b2);
            await ctx.SaveChangesAsync();
        });

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "User");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Guid", userId.ToString());

        // Act
        using var response = await HttpClient.GetAsync("/v1/bookings?page=1&pageSize=10", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<BookingResponseDto>>(DefaultJsonOptions, TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task PostBookingCancel_WithCancellationReason_ShouldApplyReason()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var eventExist = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        await ExecuteDbContextAsync(async ctx =>
        {
            ctx.EventProjections.Add(EventProjection.Create(eventExist, "Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 500m, "KZT", 1L, 100));
            var b = Booking.Create(eventExist, ownerId, 1);
            b.Confirm(500m, 500m, "KZT");
            ctx.Bookings.Add(b);
            await ctx.SaveChangesAsync();
            bookingId = b.Id;
        });

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "User");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Guid", ownerId.ToString());

        var cancelRequest = new CancelBookingRequestDto(Reason: CancellationReason.EventRescheduled);

        // Act
        using var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"/v1/bookings/{bookingId}")
        {
            Content = JsonContent.Create(cancelRequest)
        };
        using var response = await HttpClient.SendAsync(requestMessage, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var returned = await response.Content.ReadFromJsonAsync<BookingResponseDto>(DefaultJsonOptions, TestContext.Current.CancellationToken);
        returned.Should().NotBeNull();
        returned.Status.Should().Be(BookingStatus.CancellationPending);
        returned.CancellationReason.Should().Be(CancellationReason.EventRescheduled);
    }

    [Fact]
    public async Task CreateBooking_WithInvalidSeats_ShouldReturnBadRequest()
    {
        // Arrange
        var eventExist = Guid.NewGuid();
        var userId = Guid.NewGuid();

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "User");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Guid", userId.ToString());

        var invalidRequest = new CreateBookingRequestDto(Seats: 0);

        // Act
        using var response = await HttpClient.PostAsJsonAsync($"/v1/bookings/{eventExist}/book", invalidRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBooking_WhenEventNotFoundInProjection_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentEvent = Guid.NewGuid();
        var userId = Guid.NewGuid();

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "User");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Guid", userId.ToString());

        // Act
        using var response = await HttpClient.PostAsync($"/v1/bookings/{nonExistentEvent}/book", null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateBooking_WhenEventInactiveInProjection_ShouldReturnBadRequest()
    {
        // Arrange
        var inactiveEvent = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await ExecuteDbContextAsync(async ctx =>
        {
            var p = EventProjection.Create(inactiveEvent, "Inactive Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 500m, "KZT", 1L, 100);
            ctx.EventProjections.Add(p);
            await ctx.SaveChangesAsync();

            p.Cancel(version: 2L, DateTimeOffset.UtcNow);
            await ctx.SaveChangesAsync();
        });

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "User");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Guid", userId.ToString());

        // Act
        using var response = await HttpClient.PostAsync($"/v1/bookings/{inactiveEvent}/book", null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
