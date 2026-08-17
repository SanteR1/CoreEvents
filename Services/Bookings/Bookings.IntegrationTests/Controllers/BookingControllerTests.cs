using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bookings.Application.DTOs;
using Bookings.Domain.Enums;
using Bookings.IntegrationTests.Infrastructure.Auth;
using Bookings.IntegrationTests.Infrastructure.Bases;
using Bookings.IntegrationTests.Infrastructure.Factories;
using FluentAssertions;

namespace Bookings.IntegrationTests.Controllers;

public class BookingControllerTests(ApiOnlyIntegrationTestFactory factory) : ApiOnlyIntegrationTestBase(factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetBookingStatus_WithValidRequest_ShouldReturnCreateAnd()
    {
        // Arrange
        var eventExist = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        _client.DefaultRequestHeaders.Add("X-Test-Role", "User");
        _client.DefaultRequestHeaders.Add("X-Test-Guid", userId.ToString());

        // Act & Assert
        var responseCreate = await _client.PostAsync($"/bookings/{eventExist}/book", content: null, cancellationToken: TestContext.Current.CancellationToken);


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
        returnedBooking.EventId.Should().Be(eventExist);
    }

    [Fact]
    public async Task PostBookingCancel_WithNotBookingOwner_ShouldReturnHttpStatusCodeForbidden()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var hackUserId = Guid.NewGuid();
        var eventExist = Guid.NewGuid();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        _client.DefaultRequestHeaders.Add("X-Test-Role", "User");
        _client.DefaultRequestHeaders.Add("X-Test-Guid", ownerId.ToString());

        // Act & Assert
        var responseCreate = await _client.PostAsync($"/bookings/{eventExist}/book", content: null, cancellationToken: TestContext.Current.CancellationToken);

        responseCreate.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var returnedCreate = await responseCreate.Content.ReadFromJsonAsync<BookingResponseDto>(DefaultJsonOptions, TestContext.Current.CancellationToken);

        returnedCreate.Should().NotBeNull();
        returnedCreate.Id.Should().NotBe(Guid.Empty);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        _client.DefaultRequestHeaders.Add("X-Test-Role", "User");
        _client.DefaultRequestHeaders.Add("X-Test-Guid", hackUserId.ToString());

        var response = await _client.DeleteAsync($"/bookings/{returnedCreate.Id}", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostBookingCancel_WithRoleAdminAndNotBookingOwner_ShouldRequestCanceledBookingAndReturnHttpStatusCodeNoContent()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var eventExist = Guid.NewGuid();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        _client.DefaultRequestHeaders.Add("X-Test-Role", "User");
        _client.DefaultRequestHeaders.Add("X-Test-Guid", ownerId.ToString());

        // Act & Assert
        var responseCreate = await _client.PostAsync($"/bookings/{eventExist}/book", content: null, cancellationToken: TestContext.Current.CancellationToken);

        responseCreate.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var returnedCreate = await responseCreate.Content.ReadFromJsonAsync<BookingResponseDto>(DefaultJsonOptions, TestContext.Current.CancellationToken);

        returnedCreate.Should().NotBeNull();
        returnedCreate.Id.Should().NotBe(Guid.Empty);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        _client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        _client.DefaultRequestHeaders.Add("X-Test-Guid", adminId.ToString());

        var response = await _client.DeleteAsync($"/bookings/{returnedCreate.Id}", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
