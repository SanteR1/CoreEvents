using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CoreEvents.Application.DTOs;
using CoreEvents.Application.Services;
using CoreEvents.Domain.Entities;
using CoreEvents.Domain.Enums;
using CoreEvents.IntegrationTests.Infrastructure.Bases;
using CoreEvents.IntegrationTests.Infrastructure.Factories;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace CoreEvents.IntegrationTests.BackgroundServices;

public class BookingProcessingHostTests(IntegrationTestFactory factory) : SharedIntegrationTestBase(factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task E2E_ProcessBooking_ShouldBeProcessedAndConfirmedByBackgroundService()
    {
        // Arrange
        var eventId = await ExecuteDbContextAsync(async db =>
        {
            var testEvent = Event.Create("E2E Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), 10);
            db.Events.Add(testEvent);
            await db.SaveChangesAsync();
            return testEvent.Id;
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
        var token = await loginResponse.Content.ReadFromJsonAsync<string>(TestContext.Current.CancellationToken);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);
        
        var response = await _client.PostAsync($"/events/{eventId}/book", content: null, cancellationToken: TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var booking = await response.Content.ReadFromJsonAsync<BookingResponseDto>(DefaultJsonOptions, TestContext.Current.CancellationToken);
        booking.Should().NotBeNull();

        // Act
        var timeout = TimeSpan.FromSeconds(15);
        var isProcessed = await WaitUntilAsync(
            condition: async () =>
            {
                return await ExecuteDbContextAsync(async db =>
                {
                    var existBooking = await db.Bookings.FindAsync(booking.Id);
                    return existBooking != null && existBooking.Status == BookingStatus.Confirmed;
                });
            },
            timeout: timeout,
            pollingInterval: TimeSpan.FromMilliseconds(100),
            testCancellationToken: TestContext.Current.CancellationToken);


        // Assert
        isProcessed.Should().BeTrue("Фоновая служба должна была подтвердить бронь в течение {0} секунд", timeout.TotalSeconds);

        await ExecuteDbContextAsync(async db =>
        {
            var processedBooking = await db.Bookings.FindAsync(booking.Id);
            var processedEvent = await db.Events.FindAsync(eventId);
            processedBooking.Should().NotBeNull();
            processedBooking!.Status.Should().Be(BookingStatus.Confirmed);
            processedBooking!.ProcessedAt.Should().NotBeNull();
            processedEvent!.AvailableSeats.Should().Be(9);
        });
    }
}