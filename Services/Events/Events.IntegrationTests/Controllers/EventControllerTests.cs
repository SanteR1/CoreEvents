using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;
using Events.Application.DTOs;
using Events.Domain.Entities;
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
    public async Task UpdateEvent_WithRoleAdmin_ShouldReturnHttpStatusCodeOk()
    {
        // Arrange
        var startAt = DateTime.UtcNow.AddDays(2);
        var endAt = DateTime.UtcNow.AddDays(2).AddHours(2);

        var eventCreateDto = new EventCreateDto(
            Title: "Event Test",
            StartAt: startAt,
            EndAt: endAt,
            TotalSeats: 15,
            Description: "Test Description",
            Price: 1000m,
            Currency: "KZT"
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        // Act
        using var responseCreate = await HttpClient.PostAsJsonAsync("/v1/events", eventCreateDto, TestContext.Current.CancellationToken);

        // Assert
        responseCreate.StatusCode.Should().Be(HttpStatusCode.Created);

        var returnedEvent = await responseCreate.Content.ReadFromJsonAsync<EventResponseDto>(TestContext.Current.CancellationToken);
        returnedEvent.Should().NotBeNull();
        returnedEvent.Id.Should().NotBeEmpty();

        // Act
        var eventUpdate = new EventUpdateDto(
            Title: "Update Test",
            StartAt: DateTime.UtcNow.AddDays(5),
            EndAt: DateTime.UtcNow.AddDays(5).AddHours(2),
            Description: "Update Description",
            Price: 1500m,
            Currency: "KZT"
        );
        using var response = await HttpClient.PutAsJsonAsync($"/v1/events/{returnedEvent.Id}", eventUpdate, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedEvent = await response.Content.ReadFromJsonAsync<EventResponseDto>(TestContext.Current.CancellationToken);
        updatedEvent.Should().NotBeNull();
        updatedEvent.Title.Should().Be("Update Test");
        updatedEvent.Price.Should().Be(1500m);

        await ExecuteDbContextAsync(async db =>
        {
            var eventInDb = await db.Events.FindAsync(returnedEvent.Id, TestContext.Current.CancellationToken);

            eventInDb.Should().NotBeNull();
            eventInDb.Id.Should().NotBeEmpty();
            eventInDb.Title.Should().Be("Update Test");
            eventInDb.Description.Should().Be("Update Description");
            eventInDb.Price.Should().Be(1500m);
            eventInDb.Currency.Should().Be("KZT");
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
        responseCreate.StatusCode.Should().Be(HttpStatusCode.Created);

        var returnedEvent = await responseCreate.Content.ReadFromJsonAsync<EventResponseDto>(TestContext.Current.CancellationToken);
        returnedEvent.Should().NotBeNull();

        using var response = await HttpClient.DeleteAsync($"/v1/events/{returnedEvent.Id}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await ExecuteDbContextAsync(async db =>
        {
            var eventInDb = await db.Events.FindAsync(returnedEvent.Id, TestContext.Current.CancellationToken);
            eventInDb.Should().NotBeNull();
            eventInDb.IsActive.Should().BeFalse();
        });
    }

    [Fact]
    public async Task CancelEvent_WithRoleAdmin_ShouldReturnHttpStatusCodeNoContent()
    {
        // Arrange
        var eventCreateDto = new EventCreateDto(
            Title: "Cancel Test",
            StartAt: DateTime.UtcNow.AddDays(2),
            EndAt: DateTime.UtcNow.AddDays(2).AddHours(2),
            TotalSeats: 10
        );

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        using var responseCreate = await HttpClient.PostAsJsonAsync("/v1/events", eventCreateDto, TestContext.Current.CancellationToken);
        responseCreate.StatusCode.Should().Be(HttpStatusCode.Created);

        var returnedEvent = await responseCreate.Content.ReadFromJsonAsync<EventResponseDto>(TestContext.Current.CancellationToken);
        returnedEvent.Should().NotBeNull();

        // Act
        using var response = await HttpClient.PostAsync($"/v1/events/{returnedEvent.Id}/cancel", null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await ExecuteDbContextAsync(async db =>
        {
            var eventInDb = await db.Events.FindAsync(returnedEvent.Id, TestContext.Current.CancellationToken);
            eventInDb.Should().NotBeNull();
            eventInDb.IsActive.Should().BeFalse();
        });
    }

    [Fact]
    public async Task GetReservations_WithRoleAdmin_ShouldReturnOk()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        // Act
        using var response = await HttpClient.GetAsync($"/v1/events/{eventId}/reservations", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reservations = await response.Content.ReadFromJsonAsync<IReadOnlyList<SeatReservationResponseDto>>(TestContext.Current.CancellationToken);
        reservations.Should().NotBeNull();
    }

    [Fact]
    public async Task GetReservations_WithRoleUser_ShouldReturnForbidden()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "User");

        // Act
        using var response = await HttpClient.GetAsync($"/v1/events/{eventId}/reservations", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetEvents_WithPriceRangeFilter_ShouldReturnOkAndFilterEvents()
    {
        // Arrange
        var lowPriceEvent = Event.Create(
            "Cheap Event",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(2),
            totalSeats: 10,
            price: 500m,
            currency: "KZT");

        var highPriceEvent = Event.Create(
            "Expensive Event",
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(2).AddHours(2),
            totalSeats: 10,
            price: 2500m,
            currency: "KZT");

        await ExecuteDbContextAsync(async db =>
        {
            db.Events.AddRange(lowPriceEvent, highPriceEvent);
            await db.SaveChangesAsync();
        });

        // Act
        using var response = await HttpClient.GetAsync("/v1/events?minPrice=1000&maxPrice=3000&currency=KZT", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResult<EventResponseDto>>(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result.Items.Should().Contain(e => e.Id == highPriceEvent.Id);
        result.Items.Should().NotContain(e => e.Id == lowPriceEvent.Id);
    }

    [Fact]
    public async Task UpdateEvent_WithNegativePrice_ShouldReturnBadRequest()
    {
        // Arrange
        var @event = Event.Create(
            "Valid Event",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(2),
            totalSeats: 10,
            price: 1000m,
            currency: "KZT");

        await ExecuteDbContextAsync(async db =>
        {
            db.Events.Add(@event);
            await db.SaveChangesAsync();
        });

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme, "token");
        HttpClient.DefaultRequestHeaders.Add("X-Test-Role", "Admin");

        var invalidUpdate = new EventUpdateDto(
            Title: "Invalid Price Event",
            StartAt: DateTime.UtcNow.AddDays(1),
            EndAt: DateTime.UtcNow.AddDays(1).AddHours(2),
            Price: -500m);

        // Act
        using var response = await HttpClient.PutAsJsonAsync($"/v1/events/{@event.Id}", invalidUpdate, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
