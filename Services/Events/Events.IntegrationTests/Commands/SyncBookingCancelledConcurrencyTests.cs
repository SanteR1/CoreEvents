using AwesomeAssertions;
using Events.Application.Commands;
using Events.Domain.Entities;
using Events.Domain.Enums;
using Events.IntegrationTests.Infrastructure.Bases;
using Events.IntegrationTests.Infrastructure.Factories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Events.IntegrationTests.Commands;

public class SyncBookingCancelledConcurrencyTests(ApiOnlyIntegrationTestFactory factory)
    : ApiOnlyIntegrationTestBase(factory)
{
    [Fact]
    public async Task Handle_ConcurrentCalls_SyncAndLegacyPath_ShouldReleaseSeatsExactlyOnce_AndLoserBecomesNoOp()
    {
        // Arrange: Мероприятие на 10 мест, 3 места забронировано (осталось 7)
        var eventId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var @event = Event.Create("Concurrency Test Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), now, 10);
        // Зарезервируем 3 места
        @event.TryReserveSeats(bookingId, 3, now);
        @event.AvailableSeats.Should().Be(7);

        var reservation = SeatReservation.CreateReserved(
            bookingId: bookingId,
            eventId: @event.Id,
            seats: 3,
            unitPrice: 1000m,
            currency: "KZT",
            discountAmount: 0m,
            priceVersion: 1L,
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(15),
            correlationId: Guid.NewGuid(),
            causationId: Guid.NewGuid());

        await ExecuteDbContextAsync(async db =>
        {
            db.Events.Add(@event);
            db.SeatReservations.Add(reservation);
            await db.SaveChangesAsync();
        });

        // Act: Параллельный запуск двух конкурирующих путей отмены
        // Путь 1: SyncBookingCancelledCommand (из Kafka топика booking-topic)
        // Путь 2: ReleaseSeatsCommand (из Legacy Saga)
        using var barrier = new Barrier(2);

        var syncTask = Task.Run(async () =>
        {
            using var scope = Factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            barrier.SignalAndWait(TestContext.Current.CancellationToken);
            await mediator.Send(new SyncBookingCancelledCommand(bookingId, @event.Id), TestContext.Current.CancellationToken);
        }, TestContext.Current.CancellationToken);

        var legacyTask = Task.Run(async () =>
        {
            using var scope = Factory.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            barrier.SignalAndWait(TestContext.Current.CancellationToken);
            await mediator.Send(new ReleaseSeatsCommand(bookingId, @event.Id, 3), TestContext.Current.CancellationToken);
        }, TestContext.Current.CancellationToken);

        await Task.WhenAll(syncTask, legacyTask);

        // Assert: Места должны быть возвращены ровно 1 раз, статус Released
        await ExecuteDbContextAsync(async db =>
        {
            var updatedEvent = await db.Events.FirstAsync(e => e.Id == @event.Id);
            var updatedReservation = await db.SeatReservations.FirstAsync(r => r.BookingId == bookingId);

            updatedReservation.Status.Should().Be(SeatsReservationStatus.Released);
            updatedEvent.AvailableSeats.Should().Be(10, "Места должны вернуться ровно один раз, без двойного возврата");
        });
    }

    [Fact]
    public async Task Handle_ConcurrentCallsSeries_50Iterations_ShouldNeverDoubleReleaseOrCrash()
    {
        for (int i = 0; i < 50; i++)
        {
            var eventId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var @event = Event.Create($"Race {i}", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), now, 10);
            @event.TryReserveSeats(bookingId, 3, now);

            var reservation = SeatReservation.CreateReserved(
                bookingId: bookingId,
                eventId: @event.Id,
                seats: 3,
                unitPrice: 1000m,
                currency: "KZT",
                discountAmount: 0m,
                priceVersion: 1L,
                expiresAt: DateTimeOffset.UtcNow.AddMinutes(15),
                correlationId: Guid.NewGuid(),
                causationId: Guid.NewGuid());

            await ExecuteDbContextAsync(async db =>
            {
                db.Events.Add(@event);
                db.SeatReservations.Add(reservation);
                await db.SaveChangesAsync();
            });

            using var barrier = new Barrier(2);

            var syncTask = Task.Run(async () =>
            {
                using var scope = Factory.Services.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                barrier.SignalAndWait(TestContext.Current.CancellationToken);
                await mediator.Send(new SyncBookingCancelledCommand(bookingId, @event.Id), TestContext.Current.CancellationToken);
            }, TestContext.Current.CancellationToken);

            var legacyTask = Task.Run(async () =>
            {
                using var scope = Factory.Services.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                barrier.SignalAndWait(TestContext.Current.CancellationToken);
                await mediator.Send(new ReleaseSeatsCommand(bookingId, @event.Id, 3), TestContext.Current.CancellationToken);
            }, TestContext.Current.CancellationToken);

            await Task.WhenAll(syncTask, legacyTask);

            await ExecuteDbContextAsync(async db =>
            {
                var updatedEvent = await db.Events.FirstAsync(e => e.Id == @event.Id);
                var updatedReservation = await db.SeatReservations.FirstAsync(r => r.BookingId == bookingId);

                updatedReservation.Status.Should().Be(SeatsReservationStatus.Released, $"Итерация {i}: статус должен быть Released");
                updatedEvent.AvailableSeats.Should().Be(10, $"Итерация {i}: доступно должно быть ровно 10 мест (без двойного возврата)");
            });
        }
    }

    [Fact]
    public async Task Handle_AtomicCommit_BothReservationAndEventCommittedTogether()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var @event = Event.Create("Atomic Commit Event", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddHours(2), now, 20);
        @event.TryReserveSeats(bookingId, 5, now);

        var reservation = SeatReservation.CreateReserved(
            bookingId: bookingId,
            eventId: @event.Id,
            seats: 5,
            unitPrice: 2000m,
            currency: "KZT",
            discountAmount: 0m,
            priceVersion: 1L,
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(15),
            correlationId: Guid.NewGuid(),
            causationId: Guid.NewGuid());

        await ExecuteDbContextAsync(async db =>
        {
            db.Events.Add(@event);
            db.SeatReservations.Add(reservation);
            await db.SaveChangesAsync();
        });

        // Act
        await ExecuteScopeAsync(async sp =>
        {
            var mediator = sp.GetRequiredService<IMediator>();
            await mediator.Send(new SyncBookingCancelledCommand(bookingId, @event.Id), TestContext.Current.CancellationToken);
        });

        // Assert: Оба объекта обновлены в едином коммите
        await ExecuteDbContextAsync(async db =>
        {
            var updatedEvent = await db.Events.FirstAsync(e => e.Id == @event.Id);
            var updatedReservation = await db.SeatReservations.FirstAsync(r => r.BookingId == bookingId);

            updatedReservation.Status.Should().Be(SeatsReservationStatus.Released);
            updatedEvent.AvailableSeats.Should().Be(20);
        });
    }

    [Fact]
    public async Task CheckConstraint_ShouldRejectAvailableSeatsGreaterThanTotalSeats_InPostgres()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var startAt = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd HH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
            var endAt = DateTime.UtcNow.AddDays(1).AddHours(1).ToString("yyyy-MM-dd HH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
            int totalSeats = 10;
            int invalidAvailableSeats = 11; // Больше total_seats

            var sql = $@"
                INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats"")
                VALUES ('{eventId}', 'Invalid Available Seats', '{startAt}', '{endAt}', {totalSeats}, {invalidAvailableSeats});";

            // Act
            Func<Task> action = async () => await db.Database.ExecuteSqlRawAsync(sql, TestContext.Current.CancellationToken);

            // Assert
            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23514"); // check_violation
            ex.Which.ConstraintName.Should().Be("CK_events_available_seats_le_total");
        });
    }

    [Fact]
    public async Task CheckConstraint_ShouldRejectNegativeAvailableSeats_InPostgres()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var startAt = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd HH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
            var endAt = DateTime.UtcNow.AddDays(1).AddHours(1).ToString("yyyy-MM-dd HH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
            int totalSeats = 10;
            int negativeAvailableSeats = -1; // Отрицательное количество мест

            var sql = $@"
                INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats"")
                VALUES ('{eventId}', 'Negative Available Seats', '{startAt}', '{endAt}', {totalSeats}, {negativeAvailableSeats});";

            // Act
            Func<Task> action = async () => await db.Database.ExecuteSqlRawAsync(sql, TestContext.Current.CancellationToken);

            // Assert
            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23514"); // check_violation
            ex.Which.ConstraintName.Should().Be("CK_events_available_seats_non_negative");
        });
    }

    [Fact]
    public async Task CheckConstraint_ShouldRejectEventUpdated_WhenTotalSeatsDecreasedBelowAvailableSeats()
    {
        var @event = Event.Create("Decrease Total Seats Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), DateTimeOffset.UtcNow, 10);

        await ExecuteDbContextAsync(async db =>
        {
            db.Events.Add(@event);
            await db.SaveChangesAsync();
        });

        await ExecuteDbContextAsync(async db =>
        {
            // Пытаемся напрямую в БД уменьшить total_seats до 5, пока available_seats = 10
            var sql = $@"
                UPDATE ""events""
                SET ""total_seats"" = 5
                WHERE ""Id"" = '{@event.Id}';";

            Func<Task> action = async () => await db.Database.ExecuteSqlRawAsync(sql, TestContext.Current.CancellationToken);

            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23514"); // check_violation
            ex.Which.ConstraintName.Should().Be("CK_events_available_seats_le_total");
        });
    }
}
