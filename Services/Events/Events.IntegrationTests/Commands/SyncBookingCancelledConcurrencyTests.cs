using System.Globalization;
using System.Text.Json;
using AwesomeAssertions;
using CoreEvents.Shared.Contracts.Events;
using CoreEvents.Shared.Contracts.Serialization;
using Events.Application.Abstractions.Caching;
using Events.Application.Commands;
using Events.Domain.Entities;
using Events.Domain.Enums;
using Events.Infrastructure.Data;
using Events.IntegrationTests.Infrastructure.Bases;
using Events.IntegrationTests.Infrastructure.Factories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Serilog.Context;

namespace Events.IntegrationTests.Commands;

public class SyncBookingCancelledConcurrencyTests(ApiOnlyIntegrationTestFactory factory)
    : ApiOnlyIntegrationTestBase(factory)
{
    private static bool _isWarmedUp;
    private static readonly SemaphoreSlim _warmUpLock = new(1, 1);

    private async Task WarmUpAsync(CancellationToken ct = default)
    {
        if (_isWarmedUp) return;

        await _warmUpLock.WaitAsync(ct);
        try
        {
            if (_isWarmedUp) return;

            var testBookingId1 = Guid.NewGuid();
            var testBookingId2 = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            var testEvent = Event.Create(
                "Warmup Event",
                now.UtcDateTime.AddDays(1),
                now.UtcDateTime.AddDays(1).AddHours(1),
                now,
                10,
                price: 500m);
            testEvent.TryReserveSeats(testBookingId1, 2, now);
            testEvent.TryReserveSeats(testBookingId2, 2, now);

            var res1 = SeatReservation.CreateReserved(
                testBookingId1, testEvent.Id, 2, testEvent.Price, testEvent.Currency,
                0m, testEvent.PriceVersion, now.AddMinutes(15), Guid.NewGuid(), Guid.NewGuid());
            var res2 = SeatReservation.CreateReserved(
                testBookingId2, testEvent.Id, 2, testEvent.Price, testEvent.Currency,
                0m, testEvent.PriceVersion, now.AddMinutes(15), Guid.NewGuid(), Guid.NewGuid());

            await ExecuteDbContextAsync(async db =>
            {
                db.Events.Add(testEvent);
                db.SeatReservations.AddRange(res1, res2);
                await db.SaveChangesAsync(ct);
            });

            // Прогреваем оба хэндлера (включая JIT, Polly, сериализаторы и EF)
            using (var scope = Factory.Services.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new SyncBookingCancelledCommand(testBookingId1, testEvent.Id), ct);
            }

            using (var scope = Factory.Services.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new ReleaseSeatsCommand(testBookingId2, testEvent.Id, 2), ct);
            }

            _isWarmedUp = true;
        }
        finally
        {
            _warmUpLock.Release();
        }
    }

    private static (Event Event, SeatReservation Target, SeatReservation Other) CreateEventWithTwoReservations(
        string title,
        int totalSeats = 10,
        int targetSeats = 3,
        int otherSeats = 3,
        decimal unitPrice = 1000m,
        Guid? targetBookingId = null,
        DateTimeOffset? now = null)
    {
        var currentNow = now ?? DateTimeOffset.UtcNow;
        var targetId = targetBookingId ?? Guid.NewGuid();
        var otherId = Guid.NewGuid();

        var @event = Event.Create(
            title: title,
            startAt: currentNow.UtcDateTime.AddDays(1),
            endAt: currentNow.UtcDateTime.AddDays(1).AddHours(2),
            now: currentNow,
            totalSeats: totalSeats,
            price: unitPrice,
            currency: "KZT");

        var res1 = @event.TryReserveSeats(targetId, targetSeats, currentNow);
        res1.Should().Be(ReserveSeatsResult.Reserved, "Целевая бронь должна успешно зарезервировать места");

        var res2 = @event.TryReserveSeats(otherId, otherSeats, currentNow);
        res2.Should().Be(ReserveSeatsResult.Reserved, "Вторая бронь должна успешно зарезервировать места");

        var targetReservation = SeatReservation.CreateReserved(
            bookingId: targetId,
            eventId: @event.Id,
            seats: targetSeats,
            unitPrice: @event.Price,
            currency: @event.Currency,
            discountAmount: 0m,
            priceVersion: @event.PriceVersion,
            expiresAt: currentNow.AddMinutes(15),
            correlationId: Guid.NewGuid(),
            causationId: Guid.NewGuid());

        var otherReservation = SeatReservation.CreateReserved(
            bookingId: otherId,
            eventId: @event.Id,
            seats: otherSeats,
            unitPrice: @event.Price,
            currency: @event.Currency,
            discountAmount: 0m,
            priceVersion: @event.PriceVersion,
            expiresAt: currentNow.AddMinutes(15),
            correlationId: Guid.NewGuid(),
            causationId: Guid.NewGuid());

        return (@event, targetReservation, otherReservation);
    }

    private static async Task<(bool Success, string Diagnostics)> WaitUntilBlockedAsync(
        NpgsqlConnection monitorConn,
        int lockPid,
        TimeSpan timeout,
        CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        const string checkSql = @"
            WITH RECURSIVE blocked AS (
                SELECT pid FROM pg_stat_activity
                WHERE @lockPid = ANY(pg_blocking_pids(pid))
                UNION
                SELECT a.pid FROM pg_stat_activity a
                JOIN blocked b ON b.pid = ANY(pg_blocking_pids(a.pid))
            )
            SELECT COUNT(*) FROM blocked;";

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                await using var cmd = monitorConn.CreateCommand();
                cmd.CommandText = checkSql;
                cmd.Parameters.AddWithValue("lockPid", lockPid);

                var count = Convert.ToInt32(await cmd.ExecuteScalarAsync(cts.Token), CultureInfo.InvariantCulture);
                if (count >= 2)
                {
                    return (true, $"Оба обработчика заблокированы (blocked count: {count})");
                }

                await Task.Delay(50, cts.Token);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Таймаут опроса
        }

        // Собираем диагностику из pg_stat_activity
        await using var diagCmd = monitorConn.CreateCommand();
        diagCmd.CommandText = @"
            SELECT pid, wait_event_type, wait_event, state, query 
            FROM pg_stat_activity 
            WHERE pid <> pg_backend_pid() AND state = 'active';";

        var lines = new List<string>();
        await using var reader = await diagCmd.ExecuteReaderAsync(CancellationToken.None);
        while (await reader.ReadAsync(CancellationToken.None))
        {
            var pid = reader.GetInt32(0);
            var waitType = reader.IsDBNull(1) ? "null" : reader.GetString(1);
            var waitEvent = reader.IsDBNull(2) ? "null" : reader.GetString(2);
            var state = reader.IsDBNull(3) ? "null" : reader.GetString(3);
            var query = reader.IsDBNull(4) ? "null" : reader.GetString(4);
            lines.Add($"[pid={pid}, state={state}, wait_type={waitType}, wait_event={waitEvent}, query={query}]");
        }

        var diagReport = lines.Count > 0
            ? string.Join("; ", lines)
            : "Нет активных сессий в pg_stat_activity";

        return (false, $"Таймаут ожидания блокировок (lockPid={lockPid}). Активные сессии: {diagReport}");
    }

    private async Task AssertReleaseIntegrityAsync(
        Guid eventId,
        Guid targetBookingId,
        Guid otherBookingId,
        int expectedAvailableSeats,
        uint initialEventRowVersion,
        uint initialReservationRowVersion,
        int baselineCacheMessages,
        int baselineCancellationMessages,
        string? iterationContext = null,
        bool expectCancellationMessage = true)
    {
        var prefix = iterationContext != null ? $"{iterationContext}: " : string.Empty;

        await ExecuteDbContextAsync(async db =>
        {
            var updatedEvent = await db.Events.AsNoTracking().FirstAsync(e => e.Id == eventId);
            var targetReservation = await db.SeatReservations.AsNoTracking().FirstAsync(r => r.BookingId == targetBookingId);
            var otherReservation = await db.SeatReservations.AsNoTracking().FirstAsync(r => r.BookingId == otherBookingId);

            // 1. Места и статусы
            targetReservation.Status.Should().Be(SeatsReservationStatus.Released,
                $"{prefix}целевое бронирование должно перейти в статус Released");
            otherReservation.Status.Should().Be(SeatsReservationStatus.Reserved,
                $"{prefix}посторонняя бронь не должна быть затронута (должна остаться Reserved)");
            updatedEvent.AvailableSeats.Should().Be(expectedAvailableSeats,
                $"{prefix}доступно должно быть ровно {expectedAvailableSeats} мест (без двойного возврата)");

            // 2. Outbox сообщения (дедупликация и прирост относительно baseline)
            var cacheMessages = await db.OutboxMessages.AsNoTracking()
                .Where(m => m.Key == CacheKeys.Event(eventId) && m.MessageType == OutboxService.CacheInvalidationMessageType)
                .ToListAsync();
            cacheMessages.Count.Should().Be(baselineCacheMessages + 1,
                $"{prefix}должно быть добавлено ровно 1 сообщение инвалидации кэша (SeatsReleased)");

            const string completedWireName = "event.booking-cancellation-completed.v1";
            var cancellationMessages = await db.OutboxMessages.AsNoTracking()
                .Where(m => m.Key == eventId.ToString() && m.MessageType == completedWireName)
                .ToListAsync();

            if (expectCancellationMessage)
            {
                cancellationMessages.Count.Should().Be(baselineCancellationMessages + 1,
                    $"{prefix}должно быть добавлено ровно 1 сообщение {completedWireName}");

                var targetCancellation = cancellationMessages.Single(m => m.Payload.Contains(targetBookingId.ToString(), StringComparison.Ordinal));
                var payload = JsonSerializer.Deserialize<EventBookingCancellationCompleted>(
                    targetCancellation.Payload,
                    IntegrationEventJsonOptions.Default);
                payload.Should().NotBeNull();
                payload.SeatsReleased.Should().BeTrue($"{prefix}SeatsReleased в payload должен быть true");
                payload.BookingId.Should().Be(targetBookingId);
                payload.EventId.Should().Be(eventId);
            }
            else
            {
                cancellationMessages.Count.Should().Be(baselineCancellationMessages,
                    $"{prefix}сообщение {completedWireName} НЕ должно публиковаться при вызове только SyncBookingCancelledCommand (дельта ровно 0)");
            }

            // 3. Вторичная проверка RowVersion (xmin)
            updatedEvent.RowVersion.Should().NotBe(initialEventRowVersion,
                $"{prefix}xmin события должен измениться");
            targetReservation.RowVersion.Should().NotBe(initialReservationRowVersion,
                $"{prefix}xmin целевой резервации должен измениться");
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Handle_ConcurrentCalls_DeterministicRace_WithRowLock_ShouldHandleConflictAndResolveCleanly(int iteration)
    {
        await ExecuteDeterministicRaceAsync(iteration, TestContext.Current.CancellationToken);
    }

    [Theory]
    [Trait("Category", "LongRunning")]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    public async Task Handle_ConcurrentCalls_DeterministicRace_ExtendedTenIterations_ShouldHandleConflictAndResolveCleanly(int iteration)
    {
        await ExecuteDeterministicRaceAsync(iteration, TestContext.Current.CancellationToken);
    }

    private async Task ExecuteDeterministicRaceAsync(int iteration, CancellationToken ct)
    {
        await WarmUpAsync(ct);

        var targetBookingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var (@event, targetReservation, otherReservation) = CreateEventWithTwoReservations(
            title: $"Deterministic Race Event #{iteration}",
            totalSeats: 10,
            targetSeats: 3,
            otherSeats: 3,
            unitPrice: 1000m,
            targetBookingId: targetBookingId,
            now: now);

        var (initialEventRowVersion, initialReservationRowVersion) = await ExecuteDbContextAsync(async db =>
        {
            db.Events.Add(@event);
            db.SeatReservations.AddRange(targetReservation, otherReservation);
            await db.SaveChangesAsync(ct);
            return (
                db.Entry(@event).Property(e => e.RowVersion).CurrentValue,
                db.Entry(targetReservation).Property(r => r.RowVersion).CurrentValue
            );
        });

        var (baselineCache, baselineCancellation) = await ExecuteDbContextAsync(async db =>
        {
            var cacheCount = await db.OutboxMessages.CountAsync(
                m => m.Key == CacheKeys.Event(@event.Id) && m.MessageType == OutboxService.CacheInvalidationMessageType,
                ct);
            var cancelCount = await db.OutboxMessages.CountAsync(
                m => m.Key == @event.Id.ToString() && m.MessageType == "event.booking-cancellation-completed.v1",
                ct);
            return (cacheCount, cancelCount);
        });

        await using var lockConn = new NpgsqlConnection(Factory.ConnectionString);
        await lockConn.OpenAsync(ct);
        await using var lockTx = await lockConn.BeginTransactionAsync(ct);

        int lockPid;
        await using (var pidCmd = lockConn.CreateCommand())
        {
            pidCmd.Transaction = lockTx;
            pidCmd.CommandText = "SELECT pg_backend_pid();";
            lockPid = Convert.ToInt32(await pidCmd.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
        }

        // Захватываем блокировку на обе строки
        await using (var lockCmd = lockConn.CreateCommand())
        {
            lockCmd.Transaction = lockTx;
            lockCmd.CommandText = @"
                SELECT ""Id"" FROM events WHERE ""Id"" = @eventId FOR UPDATE;
                SELECT booking_id FROM event_seat_reservations WHERE booking_id = @bookingId FOR UPDATE;";
            lockCmd.Parameters.AddWithValue("eventId", @event.Id);
            lockCmd.Parameters.AddWithValue("bookingId", targetBookingId);
            await lockCmd.ExecuteNonQueryAsync(ct);
        }

        using var scope1 = Factory.Services.CreateScope();
        var mediator1 = scope1.ServiceProvider.GetRequiredService<IMediator>();
        var db1 = scope1.ServiceProvider.GetRequiredService<EventsDbContext>();
        await db1.Database.OpenConnectionAsync(ct);

        using var scope2 = Factory.Services.CreateScope();
        var mediator2 = scope2.ServiceProvider.GetRequiredService<IMediator>();
        var db2 = scope2.ServiceProvider.GetRequiredService<EventsDbContext>();
        await db2.Database.OpenConnectionAsync(ct);

        await using var monitorConn = new NpgsqlConnection(Factory.ConnectionString);
        await monitorConn.OpenAsync(ct);

        Exception? syncException = null;
        Exception? legacyException = null;
        var startSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? syncTask = null;
        Task? legacyTask = null;

        try
        {
            syncTask = Task.Run(async () =>
            {
                try
                {
                    using (LogContext.PushProperty("DomainEventId", @event.Id.ToString()))
                    using (LogContext.PushProperty("EventId", @event.Id.ToString()))
                    {
                        await startSignal.Task;
                        await mediator1.Send(new SyncBookingCancelledCommand(targetBookingId, @event.Id), ct);
                    }
                }
                catch (Exception ex)
                {
                    syncException = ex;
                }
            }, ct);

            legacyTask = Task.Run(async () =>
            {
                try
                {
                    using (LogContext.PushProperty("DomainEventId", @event.Id.ToString()))
                    using (LogContext.PushProperty("EventId", @event.Id.ToString()))
                    {
                        await startSignal.Task;
                        await mediator2.Send(new ReleaseSeatsCommand(targetBookingId, @event.Id, 3), ct);
                    }
                }
                catch (Exception ex)
                {
                    legacyException = ex;
                }
            }, ct);

            startSignal.SetResult();

            var (bothWaiting, diag) = await WaitUntilBlockedAsync(monitorConn, lockPid, TimeSpan.FromSeconds(20), ct);
            bothWaiting.Should().BeTrue($"Оба хэндлера должны быть заблокированы в PostgreSQL. Диагностика: {diag}");

            // Снимаем блокировку: победитель коммитит, проигравший ретраится через Polly
            await lockTx.RollbackAsync(ct);
            await lockConn.CloseAsync();

            await Task.WhenAll(syncTask, legacyTask).WaitAsync(TimeSpan.FromSeconds(30), ct);
        }
        finally
        {
            startSignal.TrySetCanceled(CancellationToken.None);

            if (lockTx.Connection != null)
            {
                try { await lockTx.RollbackAsync(CancellationToken.None); } catch (Exception ex) { TestContext.Current.SendDiagnosticMessage($"Rollback failed: {ex.Message}"); }
            }
            if (lockConn.State == System.Data.ConnectionState.Open)
            {
                try { await lockConn.CloseAsync(); } catch (Exception ex) { TestContext.Current.SendDiagnosticMessage($"Close failed: {ex.Message}"); }
            }
            if (syncTask != null && legacyTask != null)
            {
                try
                {
                    await Task.WhenAll(syncTask, legacyTask).WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
                }
                catch (Exception ex)
                {
                    TestContext.Current.SendDiagnosticMessage($"Background task drain failed: {ex.Message}");
                }
            }
        }

        syncException.Should().BeNull("SyncBookingCancelledCommand не должна завершаться с ошибкой");
        legacyException.Should().BeNull("ReleaseSeatsCommand не должна завершаться с ошибкой");

        // Доказательство факта ретрая: проигравший поток зафиксировал конфликт и выполнил ровно 1 ретрай
        var retryEntries = Factory.ConcurrencyLogCollector.Entries
            .Where(e => string.Equals(e.EventId, @event.Id.ToString(), StringComparison.OrdinalIgnoreCase))
            .ToList();
        retryEntries.Should().ContainSingle(
            "Проигравший поток обязан был зафиксировать конфликт конкуренции и выполнить ровно 1 ретрай");

        await AssertReleaseIntegrityAsync(
            @event.Id,
            targetBookingId,
            otherReservation.BookingId,
            expectedAvailableSeats: 7,
            initialEventRowVersion,
            initialReservationRowVersion,
            baselineCache,
            baselineCancellation);
    }

    [Fact]
    [Trait("Category", "LongRunning")]
    public async Task Handle_ConcurrentCallsSeries_ShouldNeverDoubleReleaseOrCrash()
    {
        await WarmUpAsync(TestContext.Current.CancellationToken);

        const int iterations = 20;

        for (int i = 0; i < iterations; i++)
        {
            var targetBookingId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var (@event, targetReservation, otherReservation) = CreateEventWithTwoReservations(
                title: $"Race {i}",
                totalSeats: 10,
                targetSeats: 3,
                otherSeats: 3,
                unitPrice: 1000m,
                targetBookingId: targetBookingId,
                now: now);

            var (initialEventRowVersion, initialReservationRowVersion) = await ExecuteDbContextAsync(async db =>
            {
                db.Events.Add(@event);
                db.SeatReservations.AddRange(targetReservation, otherReservation);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
                return (
                    db.Entry(@event).Property(e => e.RowVersion).CurrentValue,
                    db.Entry(targetReservation).Property(r => r.RowVersion).CurrentValue
                );
            });

            var (baselineCache, baselineCancellation) = await ExecuteDbContextAsync(async db =>
            {
                var cacheCount = await db.OutboxMessages.CountAsync(
                    m => m.Key == CacheKeys.Event(@event.Id) && m.MessageType == OutboxService.CacheInvalidationMessageType,
                    TestContext.Current.CancellationToken);
                var cancelCount = await db.OutboxMessages.CountAsync(
                    m => m.Key == @event.Id.ToString() && m.MessageType == "event.booking-cancellation-completed.v1",
                    TestContext.Current.CancellationToken);
                return (cacheCount, cancelCount);
            });

            using var scope1 = Factory.Services.CreateScope();
            var mediator1 = scope1.ServiceProvider.GetRequiredService<IMediator>();
            var db1 = scope1.ServiceProvider.GetRequiredService<EventsDbContext>();
            await db1.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);

            using var scope2 = Factory.Services.CreateScope();
            var mediator2 = scope2.ServiceProvider.GetRequiredService<IMediator>();
            var db2 = scope2.ServiceProvider.GetRequiredService<EventsDbContext>();
            await db2.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);

            Exception? syncException = null;
            Exception? legacyException = null;
            var startSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task? syncTask = null;
            Task? legacyTask = null;

            try
            {
                syncTask = Task.Run(async () =>
                {
                    try
                    {
                        using (LogContext.PushProperty("DomainEventId", @event.Id.ToString()))
                        using (LogContext.PushProperty("EventId", @event.Id.ToString()))
                        {
                            await startSignal.Task;
                            await mediator1.Send(new SyncBookingCancelledCommand(targetBookingId, @event.Id), TestContext.Current.CancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        syncException = ex;
                    }
                }, TestContext.Current.CancellationToken);

                legacyTask = Task.Run(async () =>
                {
                    try
                    {
                        using (LogContext.PushProperty("DomainEventId", @event.Id.ToString()))
                        using (LogContext.PushProperty("EventId", @event.Id.ToString()))
                        {
                            await startSignal.Task;
                            await mediator2.Send(new ReleaseSeatsCommand(targetBookingId, @event.Id, 3), TestContext.Current.CancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        legacyException = ex;
                    }
                }, TestContext.Current.CancellationToken);

                startSignal.SetResult();

                await Task.WhenAll(syncTask, legacyTask).WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
            }
            finally
            {
                startSignal.TrySetCanceled(CancellationToken.None);
                if (syncTask != null && legacyTask != null)
                {
                    try
                    {
                        await Task.WhenAll(syncTask, legacyTask).WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        TestContext.Current.SendDiagnosticMessage($"Background task drain failed: {ex.Message}");
                    }
                }
            }

            syncException.Should().BeNull($"Итерация {i}: SyncBookingCancelledCommand не должна завершаться с ошибкой");
            legacyException.Should().BeNull($"Итерация {i}: ReleaseSeatsCommand не должна завершаться с ошибкой");

            // Информационная фиксация ретраев в серии (без флакующих assertion)
            var iterationRetries = Factory.ConcurrencyLogCollector.Entries
                .Count(e => string.Equals(e.EventId, @event.Id.ToString(), StringComparison.OrdinalIgnoreCase));
            if (iterationRetries > 0)
            {
                TestContext.Current.SendDiagnosticMessage($"Итерация {i}: зафиксировано ретраев конкуренции: {iterationRetries}");
            }

            await AssertReleaseIntegrityAsync(
                @event.Id,
                targetBookingId,
                otherReservation.BookingId,
                expectedAvailableSeats: 7,
                initialEventRowVersion,
                initialReservationRowVersion,
                baselineCache,
                baselineCancellation,
                iterationContext: $"Итерация {i}");
        }
    }

    [Fact]
    public async Task Handle_ConcurrentCalls_SyncAndLegacyPath_ShouldReleaseSeatsExactlyOnce_AndLoserBecomesNoOp()
    {
        await WarmUpAsync(TestContext.Current.CancellationToken);

        var targetBookingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var (@event, targetReservation, otherReservation) = CreateEventWithTwoReservations(
            title: "Concurrency Single Event",
            totalSeats: 10,
            targetSeats: 3,
            otherSeats: 3,
            unitPrice: 1000m,
            targetBookingId: targetBookingId,
            now: now);

        var (initialEventRowVersion, initialReservationRowVersion) = await ExecuteDbContextAsync(async db =>
        {
            db.Events.Add(@event);
            db.SeatReservations.AddRange(targetReservation, otherReservation);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            return (
                db.Entry(@event).Property(e => e.RowVersion).CurrentValue,
                db.Entry(targetReservation).Property(r => r.RowVersion).CurrentValue
            );
        });

        var (baselineCache, baselineCancellation) = await ExecuteDbContextAsync(async db =>
        {
            var cacheCount = await db.OutboxMessages.CountAsync(
                m => m.Key == CacheKeys.Event(@event.Id) && m.MessageType == OutboxService.CacheInvalidationMessageType,
                TestContext.Current.CancellationToken);
            var cancelCount = await db.OutboxMessages.CountAsync(
                m => m.Key == @event.Id.ToString() && m.MessageType == "event.booking-cancellation-completed.v1",
                TestContext.Current.CancellationToken);
            return (cacheCount, cancelCount);
        });

        using var scope1 = Factory.Services.CreateScope();
        var mediator1 = scope1.ServiceProvider.GetRequiredService<IMediator>();
        var db1 = scope1.ServiceProvider.GetRequiredService<EventsDbContext>();
        await db1.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);

        using var scope2 = Factory.Services.CreateScope();
        var mediator2 = scope2.ServiceProvider.GetRequiredService<IMediator>();
        var db2 = scope2.ServiceProvider.GetRequiredService<EventsDbContext>();
        await db2.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);

        Exception? syncException = null;
        Exception? legacyException = null;
        var startSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? syncTask = null;
        Task? legacyTask = null;

        try
        {
            syncTask = Task.Run(async () =>
            {
                try
                {
                    using (LogContext.PushProperty("DomainEventId", @event.Id.ToString()))
                    using (LogContext.PushProperty("EventId", @event.Id.ToString()))
                    {
                        await startSignal.Task;
                        await mediator1.Send(new SyncBookingCancelledCommand(targetBookingId, @event.Id), TestContext.Current.CancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    syncException = ex;
                }
            }, TestContext.Current.CancellationToken);

            legacyTask = Task.Run(async () =>
            {
                try
                {
                    using (LogContext.PushProperty("DomainEventId", @event.Id.ToString()))
                    using (LogContext.PushProperty("EventId", @event.Id.ToString()))
                    {
                        await startSignal.Task;
                        await mediator2.Send(new ReleaseSeatsCommand(targetBookingId, @event.Id, 3), TestContext.Current.CancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    legacyException = ex;
                }
            }, TestContext.Current.CancellationToken);

            startSignal.SetResult();

            await Task.WhenAll(syncTask, legacyTask).WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        }
        finally
        {
            startSignal.TrySetCanceled(CancellationToken.None);
            if (syncTask != null && legacyTask != null)
            {
                try
                {
                    await Task.WhenAll(syncTask, legacyTask).WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
                }
                catch (Exception ex)
                {
                    TestContext.Current.SendDiagnosticMessage($"Background task drain failed: {ex.Message}");
                }
            }
        }

        syncException.Should().BeNull("SyncBookingCancelledCommand не должна завершаться с ошибкой");
        legacyException.Should().BeNull("ReleaseSeatsCommand не должна завершаться с ошибкой");

        await AssertReleaseIntegrityAsync(
            @event.Id,
            targetBookingId,
            otherReservation.BookingId,
            expectedAvailableSeats: 7,
            initialEventRowVersion,
            initialReservationRowVersion,
            baselineCache,
            baselineCancellation);
    }

    [Fact]
    public async Task Handle_SingleCall_ShouldReleaseReservationAndSeats()
    {
        // Arrange
        var targetBookingId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var (@event, targetReservation, otherReservation) = CreateEventWithTwoReservations(
            title: "Single Call Event",
            totalSeats: 10,
            targetSeats: 3,
            otherSeats: 3,
            unitPrice: 1000m,
            targetBookingId: targetBookingId,
            now: now);

        var (initialEventRowVersion, initialReservationRowVersion) = await ExecuteDbContextAsync(async db =>
        {
            db.Events.Add(@event);
            db.SeatReservations.AddRange(targetReservation, otherReservation);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            return (
                db.Entry(@event).Property(e => e.RowVersion).CurrentValue,
                db.Entry(targetReservation).Property(r => r.RowVersion).CurrentValue
            );
        });

        var (baselineCache, baselineCancellation) = await ExecuteDbContextAsync(async db =>
        {
            var cacheCount = await db.OutboxMessages.CountAsync(
                m => m.Key == CacheKeys.Event(@event.Id) && m.MessageType == OutboxService.CacheInvalidationMessageType,
                TestContext.Current.CancellationToken);
            var cancelCount = await db.OutboxMessages.CountAsync(
                m => m.Key == @event.Id.ToString() && m.MessageType == "event.booking-cancellation-completed.v1",
                TestContext.Current.CancellationToken);
            return (cacheCount, cancelCount);
        });

        // Act
        await ExecuteScopeAsync(async sp =>
        {
            var mediator = sp.GetRequiredService<IMediator>();
            await mediator.Send(new SyncBookingCancelledCommand(targetBookingId, @event.Id), TestContext.Current.CancellationToken);
        });

        // Assert: Оба объекта и Outbox обновлены корректно
        await AssertReleaseIntegrityAsync(
            @event.Id,
            targetBookingId,
            otherReservation.BookingId,
            expectedAvailableSeats: 7,
            initialEventRowVersion,
            initialReservationRowVersion,
            baselineCache,
            baselineCancellation,
            expectCancellationMessage: false);
    }
}
