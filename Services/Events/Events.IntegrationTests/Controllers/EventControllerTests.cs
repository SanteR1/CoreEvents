using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;
using Events.Application.DTOs;
using Events.IntegrationTests.Infrastructure.Auth;
using Events.IntegrationTests.Infrastructure.Bases;
using Events.IntegrationTests.Infrastructure.Factories;

namespace Events.IntegrationTests.Controllers;

public sealed class EventControllerTests(ApiOnlyIntegrationTestFactory factory) : ApiOnlyIntegrationTestBase(factory)
{
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

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        // Act
        using var response = await HttpClient.PostAsJsonAsync("/v1/events", eventCreateDto, TestContext.Current.CancellationToken);

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
    public async Task CreateEvent_WithUserRole_ShouldReturnHttpStatusCodeForbidden()
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

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "User");

        // Act
        using var response = await HttpClient.PostAsJsonAsync("/v1/events", eventCreateDto, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateEvent_WithUserAdmin_ShouldReturnHttpStatusCodeCreated()
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

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        // Act
        using var response = await HttpClient.PostAsJsonAsync("/v1/events", eventCreateDto, TestContext.Current.CancellationToken);

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
    public async Task UpdateEvent_WithUserRole_ShouldReturnHttpStatusCodeForbidden()
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

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "User");

        // Act
        using var response = await HttpClient.PutAsJsonAsync($"/v1/events/{Guid.NewGuid()}", eventCreateDto, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateEvent_WithRoleAdmin_ShouldReturnHttpStatusCodeNoContent()
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

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        // Act
        using var responseCreate = await HttpClient.PostAsJsonAsync("/v1/events", eventCreateDto, TestContext.Current.CancellationToken);

        // Assert
        responseCreate.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var returnedEvent = await responseCreate.Content.ReadFromJsonAsync<EventResponseDto>(TestContext.Current.CancellationToken);
        returnedEvent.Should().NotBeNull();
        returnedEvent.Id.Should().NotBeEmpty();

        // Act
        var eventUpdate = new EventCreateDto(
            Title: "Update Test",
            StartAt: DateTime.UtcNow.AddDays(5),
            EndAt: DateTime.UtcNow.AddDays(5).AddHours(2),
            TotalSeats: 20,
            Description: "Update Description"
        );
        using var response = await HttpClient.PutAsJsonAsync($"/v1/events/{returnedEvent.Id}", eventUpdate, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await ExecuteDbContextAsync(async db =>
        {
            var bookingInDb = await db.Events.FindAsync(returnedEvent.Id, TestContext.Current.CancellationToken);

            bookingInDb.Should().NotBeNull();
            bookingInDb.Id.Should().NotBeEmpty();
            bookingInDb.Title.Should().Be("Update Test");
            bookingInDb.Description.Should().Be("Update Description");
            bookingInDb.StartAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(5), TimeSpan.FromSeconds(2));
            bookingInDb.EndAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(5).AddHours(2), TimeSpan.FromSeconds(2));
        });
    }

    [Fact]
    public async Task DeleteEvent_WithUserRole_ShouldReturnHttpStatusCodeForbidden()
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

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "User");

        // Act
        using var response = await HttpClient.DeleteAsync($"/v1/events/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteEvent_WithRoleAdmin_ShouldReturnHttpStatusCodeNoContent()
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

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        // Act
        using var responseCreate = await HttpClient.PostAsJsonAsync("/v1/events", eventCreateDto, TestContext.Current.CancellationToken);

        // Assert
        responseCreate.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var returnedEvent = await responseCreate.Content.ReadFromJsonAsync<EventResponseDto>(TestContext.Current.CancellationToken);
        returnedEvent.Should().NotBeNull();
        returnedEvent.Id.Should().NotBeEmpty();

        // Act
        var eventUpdate = new EventCreateDto(
            Title: "Update Test",
            StartAt: DateTime.UtcNow.AddDays(5),
            EndAt: DateTime.UtcNow.AddDays(5).AddHours(2),
            TotalSeats: 20,
            Description: "Update Description"
        );
        using var response = await HttpClient.PutAsJsonAsync($"/v1/events/{returnedEvent.Id}", eventUpdate, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await ExecuteDbContextAsync(async db =>
        {
            var bookingInDb = await db.Events.FindAsync(returnedEvent.Id, TestContext.Current.CancellationToken);

            bookingInDb.Should().NotBeNull();
            bookingInDb.Id.Should().NotBeEmpty();
            bookingInDb.Title.Should().Be("Update Test");
            bookingInDb.Description.Should().Be("Update Description");
            bookingInDb.StartAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(5), TimeSpan.FromSeconds(2));
            bookingInDb.EndAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(5).AddHours(2), TimeSpan.FromSeconds(2));
        });
    }
}
