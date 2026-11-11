using System.Text.Json;
using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Registry;
using CoreEvents.Shared.Contracts.Serialization;
using Events.Application.Abstractions.Messaging;
using Events.Application.Commands;
using Events.Domain.Entities;
using Events.Domain.Enums;
using Events.IntegrationTests.Infrastructure.Bases;
using Events.IntegrationTests.Infrastructure.Factories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Events.IntegrationTests.Commands;

public class BookingReservationRequestIntegrationTests(ApiOnlyIntegrationTestFactory factory)
    : ApiOnlyIntegrationTestBase(factory)
{
    [Fact]
    public async Task Handle_WhenValidReservationRequest_ShouldProduceExactlyOneOutboxMessage_WithBookingReservationAccepted()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var testEvent = Event.Create(
            "Integration Test Concert",
            now.UtcDateTime.AddDays(2),
            now.UtcDateTime.AddDays(2).AddHours(2),
            now,
            totalSeats: 10,
            price: 1500m,
            currency: "KZT");

        await ExecuteDbContextAsync(async db =>
        {
            db.Events.Add(testEvent);
            await db.SaveChangesAsync();
        });

        // Act
        var result = await ExecuteScopeAsync(sp =>
        {
            var sender = sp.GetRequiredService<ISender>();
            return sender.Send(new BookingReservationRequestCommand(bookingId, testEvent.Id, userId, 2), CancellationToken.None);
        });

        // Assert
        result.Should().Be(InboxProcessingResult.Processed);

        await ExecuteScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<Events.Infrastructure.Data.EventsDbContext>();
            var outboxMessages = await db.OutboxMessages
                .Where(m => m.Key == testEvent.Id.ToString())
                .ToListAsync();

            outboxMessages.Should().ContainSingle(
                "При обработке бронирования в Outbox должна быть сохранена ровно одна запись ответа саге, без дубликатов из доменных хендлеров.");

            var registry = sp.GetRequiredService<IIntegrationEventRegistry>();
            outboxMessages[0].MessageType.Should().Be(registry.GetName<BookingReservationAccepted>());

            var reservation = await db.SeatReservations
                .FirstOrDefaultAsync(r => r.BookingId == bookingId);

            reservation.Should().NotBeNull();
            reservation.Status.Should().Be(SeatsReservationStatus.Reserved);
            reservation.Seats.Should().Be(2);

            var updatedEvent = await db.Events.FindAsync(testEvent.Id);
            updatedEvent.Should().NotBeNull();
            updatedEvent.AvailableSeats.Should().Be(8);
        });
    }

    [Fact]
    public async Task Handle_WhenSeatsNotAvailable_ShouldProduceExactlyOneOutboxMessage_WithBookingReservationRejected_AndSeatsNotAvailableReason()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var testEvent = Event.Create(
            "Sold Out Concert",
            now.UtcDateTime.AddDays(2),
            now.UtcDateTime.AddDays(2).AddHours(2),
            now,
            totalSeats: 2,
            price: 1500m,
            currency: "KZT");

        await ExecuteDbContextAsync(async db =>
        {
            db.Events.Add(testEvent);
            await db.SaveChangesAsync();
        });

        // Act - запрашиваем 5 мест при 2 доступных
        var result = await ExecuteScopeAsync(sp =>
        {
            var sender = sp.GetRequiredService<ISender>();
            return sender.Send(new BookingReservationRequestCommand(bookingId, testEvent.Id, userId, 5), CancellationToken.None);
        });

        // Assert
        result.Should().Be(InboxProcessingResult.Processed);

        await ExecuteScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<Events.Infrastructure.Data.EventsDbContext>();
            var outboxMessages = await db.OutboxMessages
                .Where(m => m.Key == testEvent.Id.ToString())
                .ToListAsync();

            outboxMessages.Should().ContainSingle(
                "При отказе бронирования в Outbox должна быть сохранена ровно одна запись BookingReservationRejected.");

            var registry = sp.GetRequiredService<IIntegrationEventRegistry>();
            outboxMessages[0].MessageType.Should().Be(registry.GetName<BookingReservationRejected>());

            var payload = JsonSerializer.Deserialize<BookingReservationRejected>(outboxMessages[0].Payload, IntegrationEventJsonOptions.Default);
            payload.Should().NotBeNull();
            payload.RejectionReason.Should().Be(ValidationFailureReason.SeatsNotAvailable);

            var reservation = await db.SeatReservations
                .FirstOrDefaultAsync(r => r.BookingId == bookingId);

            reservation.Should().NotBeNull();
            reservation.Status.Should().Be(SeatsReservationStatus.Rejected);
            reservation.RejectionReason.Should().Be(ValidationFailureReason.SeatsNotAvailable);

            var updatedEvent = await db.Events.FindAsync(testEvent.Id);
            updatedEvent.Should().NotBeNull();
            updatedEvent.AvailableSeats.Should().Be(2); // не изменилось
        });
    }

    [Fact]
    public async Task Handle_WhenEventIsInactive_ShouldProduceExactlyOneOutboxMessage_WithBookingReservationRejected_AndEventNotFoundReason()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var testEvent = Event.Create(
            "Inactive Concert",
            now.UtcDateTime.AddDays(2),
            now.UtcDateTime.AddDays(2).AddHours(2),
            now,
            totalSeats: 10,
            price: 1500m,
            currency: "KZT");

        await ExecuteDbContextAsync(async db =>
        {
            db.Events.Add(testEvent);
            await db.SaveChangesAsync();
        });

        await ExecuteDbContextAsync(async db =>
        {
            var e = await db.Events.FindAsync(testEvent.Id);
            e!.Cancel(now);
            await db.SaveChangesAsync();
        });

        // Act - запрашиваем места на отмененном/неактивном событии
        var result = await ExecuteScopeAsync(sp =>
        {
            var sender = sp.GetRequiredService<ISender>();
            return sender.Send(new BookingReservationRequestCommand(bookingId, testEvent.Id, userId, 2), CancellationToken.None);
        });

        // Assert
        result.Should().Be(InboxProcessingResult.Processed);

        await ExecuteScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<Events.Infrastructure.Data.EventsDbContext>();
            var outboxMessages = await db.OutboxMessages
                .Where(m => m.Key == testEvent.Id.ToString())
                .ToListAsync();

            outboxMessages.Should().ContainSingle(
                "При попытке бронирования неактивного события должна быть создана ровно одна запись BookingReservationRejected.");

            var registry = sp.GetRequiredService<IIntegrationEventRegistry>();
            outboxMessages[0].MessageType.Should().Be(registry.GetName<BookingReservationRejected>());

            var payload = JsonSerializer.Deserialize<BookingReservationRejected>(outboxMessages[0].Payload, IntegrationEventJsonOptions.Default);
            payload.Should().NotBeNull();
            payload.RejectionReason.Should().Be(ValidationFailureReason.EventNotFound);

            var reservation = await db.SeatReservations
                .FirstOrDefaultAsync(r => r.BookingId == bookingId);

            reservation.Should().NotBeNull();
            reservation.Status.Should().Be(SeatsReservationStatus.Rejected);
            reservation.RejectionReason.Should().Be(ValidationFailureReason.EventNotFound);
        });
    }

    [Fact]
    public async Task Handle_WhenReleaseReservationRequest_ShouldProduceExactlyOneOutboxMessage_WithBookingReservationReleased()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var testEvent = Event.Create(
            "Concert for Release",
            now.UtcDateTime.AddDays(2),
            now.UtcDateTime.AddDays(2).AddHours(2),
            now,
            totalSeats: 10,
            price: 1500m,
            currency: "KZT");
        testEvent.TryReserveSeats(bookingId, 3, now);

        var reservation = SeatReservation.CreateReserved(
            bookingId,
            testEvent.Id,
            seats: 3,
            unitPrice: testEvent.Price,
            currency: testEvent.Currency,
            discountAmount: 0m,
            priceVersion: testEvent.PriceVersion,
            expiresAt: now.AddMinutes(15),
            correlationId: Guid.NewGuid(),
            causationId: Guid.NewGuid());

        await ExecuteDbContextAsync(async db =>
        {
            db.Events.Add(testEvent);
            db.SeatReservations.Add(reservation);
            await db.SaveChangesAsync();
        });

        // Act - освобождаем места по запросу саги
        var result = await ExecuteScopeAsync(sp =>
        {
            var sender = sp.GetRequiredService<ISender>();
            return sender.Send(
                new BookingReservationReleaseRequestCommand(bookingId, testEvent.Id, 3, CancellationReason.UserCancelled),
                CancellationToken.None);
        });

        // Assert
        result.Should().Be(InboxProcessingResult.Processed);

        await ExecuteScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<Events.Infrastructure.Data.EventsDbContext>();
            var outboxMessages = await db.OutboxMessages
                .Where(m => m.Key == testEvent.Id.ToString())
                .ToListAsync();

            outboxMessages.Should().ContainSingle(
                "При освобождении мест бронирования должна быть создана ровно одна запись BookingReservationReleased.");

            var registry = sp.GetRequiredService<IIntegrationEventRegistry>();
            outboxMessages[0].MessageType.Should().Be(registry.GetName<BookingReservationReleased>());

            var updatedReservation = await db.SeatReservations.FirstOrDefaultAsync(r => r.BookingId == bookingId);
            updatedReservation.Should().NotBeNull();
            updatedReservation.Status.Should().Be(SeatsReservationStatus.Released);

            var updatedEvent = await db.Events.FindAsync(testEvent.Id);
            updatedEvent.Should().NotBeNull();
            updatedEvent.AvailableSeats.Should().Be(10); // места возвращены
        });
    }
}
