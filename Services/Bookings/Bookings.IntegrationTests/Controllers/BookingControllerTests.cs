using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;
using Bookings.Application.DTOs;
using Bookings.Domain.Enums;
using Bookings.IntegrationTests.Infrastructure.Auth;
using Bookings.IntegrationTests.Infrastructure.Bases;
using Bookings.IntegrationTests.Infrastructure.Factories;

namespace Bookings.IntegrationTests.Controllers;

public sealed class BookingControllerTests(ApiOnlyIntegrationTestFactory factory) : ApiOnlyIntegrationTestBase(factory)
{
    [Fact]
    public async Task GetBookingStatus_WithValidRequest_ShouldReturnCreateAnd()
    {
        // Arrange
        var eventExist = Guid.NewGuid();
        var userId = Guid.NewGuid();

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "User");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Guid", userId.ToString());

        // Act & Assert
        using var responseCreate = await HttpClient.PostAsync($"/bookings/{eventExist}/book", content: null, cancellationToken: TestContext.Current.CancellationToken);

        responseCreate.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var returnedCreate = await responseCreate.Content.ReadFromJsonAsync<BookingResponseDto>(DefaultJsonOptions, TestContext.Current.CancellationToken);

        returnedCreate.Should().NotBeNull();
        returnedCreate.Id.Should().NotBe(Guid.Empty);

        using var response = await HttpClient.GetAsync($"/bookings/{returnedCreate.Id}", cancellationToken: TestContext.Current.CancellationToken);
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

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "User");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Guid", ownerId.ToString());

        // Act & Assert
        using HttpResponseMessage responseCreate = await HttpClient.PostAsync($"/bookings/{eventExist}/book", content: null, cancellationToken: TestContext.Current.CancellationToken);

        responseCreate.StatusCode.Should().Be(HttpStatusCode.Accepted);
        BookingResponseDto? returnedCreate = await responseCreate.Content.ReadFromJsonAsync<BookingResponseDto>(DefaultJsonOptions, TestContext.Current.CancellationToken);

        returnedCreate.Should().NotBeNull();
        returnedCreate.Id.Should().NotBe(Guid.Empty);

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "User");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Guid", hackUserId.ToString());

        using HttpResponseMessage response = await HttpClient.DeleteAsync($"/bookings/{returnedCreate.Id}", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostBookingCancel_WithRoleAdminAndNotBookingOwner_ShouldRequestCanceledBookingAndReturnHttpStatusCodeNoContent()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var eventExist = Guid.NewGuid();

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "User");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Guid", ownerId.ToString());

        // Act & Assert
        using var responseCreate = await HttpClient.PostAsync($"/bookings/{eventExist}/book", content: null, cancellationToken: TestContext.Current.CancellationToken);

        responseCreate.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var returnedCreate = await responseCreate.Content.ReadFromJsonAsync<BookingResponseDto>(DefaultJsonOptions, TestContext.Current.CancellationToken);

        returnedCreate.Should().NotBeNull();
        returnedCreate.Id.Should().NotBe(Guid.Empty);

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Guid", adminId.ToString());

        using var response = await HttpClient.DeleteAsync($"/bookings/{returnedCreate.Id}", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
