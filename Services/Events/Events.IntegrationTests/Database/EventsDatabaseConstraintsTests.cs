using AwesomeAssertions;
using Events.Domain.Entities;
using Events.IntegrationTests.Infrastructure.Bases;
using Events.IntegrationTests.Infrastructure.Collections;
using Events.IntegrationTests.Infrastructure.Factories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Events.IntegrationTests.Database;

[Collection(TestCollections.ApiOnly)]
public class EventsDatabaseConstraintsTests(ApiOnlyIntegrationTestFactory factory)
    : IntegrationTestBase<ApiOnlyIntegrationTestFactory>(factory)
{
    #region CK_events_available_seats_le_total

    [Fact]
    public async Task CheckConstraint_ShouldRejectAvailableSeatsGreaterThanTotalSeats_InPostgres()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var startAt = DateTime.UtcNow.AddDays(1);
            var endAt = DateTime.UtcNow.AddDays(1).AddHours(1);
            int totalSeats = 10;
            int invalidAvailableSeats = 11; // Больше total_seats

            // Act: параметризованный запрос через ExecuteSqlInterpolatedAsync вместо интерполяции сырого SQL
            Func<Task> action = async () => await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats"")
                VALUES ({eventId}, 'Invalid Available Seats', {startAt}, {endAt}, {totalSeats}, {invalidAvailableSeats});",
                TestContext.Current.CancellationToken);

            // Assert
            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23514"); // check_violation
            ex.Which.ConstraintName.Should().Be("CK_events_available_seats_le_total");
        });
    }

    [Fact]
    public async Task CheckConstraint_ShouldRejectEventUpdated_WhenTotalSeatsDecreasedBelowAvailableSeats()
    {
        var @event = Event.Create("Decrease Total Seats Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), DateTimeOffset.UtcNow, 10);

        await ExecuteDbContextAsync(async db =>
        {
            db.Events.Add(@event);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        await ExecuteDbContextAsync(async db =>
        {
            int newTotalSeats = 5; // Меньше available_seats (10)

            // Act: параметризованный запрос через ExecuteSqlInterpolatedAsync вместо интерполяции сырого SQL
            Func<Task> action = async () => await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""events""
                SET ""total_seats"" = {newTotalSeats}
                WHERE ""Id"" = {@event.Id};",
                TestContext.Current.CancellationToken);

            // Assert
            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23514"); // check_violation
            ex.Which.ConstraintName.Should().Be("CK_events_available_seats_le_total");
        });
    }

    [Fact]
    public async Task CheckConstraint_ShouldRejectEventUpdated_WhenAvailableSeatsIncreasedAboveTotalSeats()
    {
        var @event = Event.Create("Increase Available Seats Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), DateTimeOffset.UtcNow, 10);

        await ExecuteDbContextAsync(async db =>
        {
            db.Events.Add(@event);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        await ExecuteDbContextAsync(async db =>
        {
            int invalidAvailableSeats = 15; // Больше total_seats (10)

            Func<Task> action = async () => await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""events""
                SET ""available_seats"" = {invalidAvailableSeats}
                WHERE ""Id"" = {@event.Id};",
                TestContext.Current.CancellationToken);

            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23514"); // check_violation
            ex.Which.ConstraintName.Should().Be("CK_events_available_seats_le_total");
        });
    }

    [Fact]
    public async Task CheckConstraint_ShouldAllowEvent_WhenAvailableSeatsEqualsTotalSeats()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // Arrange: граничный случай 100% свободных мест (полная емкость)
            var eventId = Guid.NewGuid();
            var startAt = DateTime.UtcNow.AddDays(1);
            var endAt = DateTime.UtcNow.AddDays(1).AddHours(2);
            int totalSeats = 10;
            int availableSeats = 10;

            // Act
            var rowsAffected = await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats"")
                VALUES ({eventId}, 'Boundary All Seats Available', {startAt}, {endAt}, {totalSeats}, {availableSeats});",
                TestContext.Current.CancellationToken);

            // Assert
            rowsAffected.Should().Be(1);

            var inDb = await db.Events.FindAsync([eventId], TestContext.Current.CancellationToken);
            inDb.Should().NotBeNull();
            inDb.AvailableSeats.Should().Be(totalSeats);
        });
    }

    [Fact]
    public async Task CheckConstraint_ShouldAllowEvent_WhenAvailableSeatsLessThanTotalSeats()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // Arrange: частично забронированное событие
            var eventId = Guid.NewGuid();
            var startAt = DateTime.UtcNow.AddDays(1);
            var endAt = DateTime.UtcNow.AddDays(1).AddHours(2);
            int totalSeats = 20;
            int availableSeats = 15;

            // Act
            var rowsAffected = await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats"")
                VALUES ({eventId}, 'Partially Booked Event', {startAt}, {endAt}, {totalSeats}, {availableSeats});",
                TestContext.Current.CancellationToken);

            // Assert
            rowsAffected.Should().Be(1);
        });
    }

    #endregion

    #region CK_events_available_seats_non_negative

    [Fact]
    public async Task CheckConstraint_ShouldRejectNegativeAvailableSeats_InPostgres()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var startAt = DateTime.UtcNow.AddDays(1);
            var endAt = DateTime.UtcNow.AddDays(1).AddHours(1);
            int totalSeats = 10;
            int negativeAvailableSeats = -1; // Отрицательное количество мест

            // Act: параметризованный запрос через ExecuteSqlInterpolatedAsync вместо интерполяции сырого SQL
            Func<Task> action = async () => await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats"")
                VALUES ({eventId}, 'Negative Available Seats', {startAt}, {endAt}, {totalSeats}, {negativeAvailableSeats});",
                TestContext.Current.CancellationToken);

            // Assert
            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23514"); // check_violation
            ex.Which.ConstraintName.Should().Be("CK_events_available_seats_non_negative");
        });
    }

    [Fact]
    public async Task CheckConstraint_ShouldRejectEventUpdated_WhenAvailableSeatsBecomesNegative()
    {
        var @event = Event.Create("Oversell Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), DateTimeOffset.UtcNow, 10);

        await ExecuteDbContextAsync(async db =>
        {
            db.Events.Add(@event);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        await ExecuteDbContextAsync(async db =>
        {
            int negativeSeats = -1; // Овербукинг ниже 0

            Func<Task> action = async () => await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""events""
                SET ""available_seats"" = {negativeSeats}
                WHERE ""Id"" = {@event.Id};",
                TestContext.Current.CancellationToken);

            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23514"); // check_violation
            ex.Which.ConstraintName.Should().Be("CK_events_available_seats_non_negative");
        });
    }

    [Fact]
    public async Task CheckConstraint_ShouldAllowEvent_WhenAvailableSeatsIsZero()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // Arrange: граничный случай Sold Out события (доступно ровно 0 мест)
            var eventId = Guid.NewGuid();
            var startAt = DateTime.UtcNow.AddDays(1);
            var endAt = DateTime.UtcNow.AddDays(1).AddHours(2);
            int totalSeats = 10;
            int zeroAvailableSeats = 0;

            // Act
            var rowsAffected = await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats"")
                VALUES ({eventId}, 'Sold Out Event', {startAt}, {endAt}, {totalSeats}, {zeroAvailableSeats});",
                TestContext.Current.CancellationToken);

            // Assert
            rowsAffected.Should().Be(1);

            var inDb = await db.Events.FindAsync([eventId], TestContext.Current.CancellationToken);
            inDb.Should().NotBeNull();
            inDb.AvailableSeats.Should().Be(0);
        });
    }

    [Fact]
    public async Task CheckConstraint_ShouldAllowEventUpdated_WhenDecreasedToZero()
    {
        var @event = Event.Create("Book Last Seat Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), DateTimeOffset.UtcNow, 5);

        await ExecuteDbContextAsync(async db =>
        {
            db.Events.Add(@event);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        await ExecuteDbContextAsync(async db =>
        {
            // Act: уменьшение доступных мест ровно до 0 (последний билет куплен)
            var rowsAffected = await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""events""
                SET ""available_seats"" = 0
                WHERE ""Id"" = {@event.Id};",
                TestContext.Current.CancellationToken);

            // Assert
            rowsAffected.Should().Be(1);

            var inDb = await db.Events.FindAsync([@event.Id], TestContext.Current.CancellationToken);
            inDb.Should().NotBeNull();
            inDb.AvailableSeats.Should().Be(0);
        });
    }

    #endregion

    #region CK_events_dates

    [Fact]
    public async Task CheckConstraint_ShouldRejectEvent_WhenStartAtEqualsEndAt()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // Arrange: нулевая длительность события (start_at == end_at)
            var eventId = Guid.NewGuid();
            var moment = DateTime.UtcNow.AddDays(1);

            // Act
            Func<Task> action = async () => await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats"")
                VALUES ({eventId}, 'Zero Duration Event', {moment}, {moment}, 10, 10);",
                TestContext.Current.CancellationToken);

            // Assert
            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23514"); // check_violation
            ex.Which.ConstraintName.Should().Be("CK_events_dates");
        });
    }

    [Fact]
    public async Task CheckConstraint_ShouldRejectEvent_WhenStartAtAfterEndAt()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // Arrange: дата окончания раньше даты начала
            var eventId = Guid.NewGuid();
            var startAt = DateTime.UtcNow.AddDays(2);
            var endAt = DateTime.UtcNow.AddDays(1);

            // Act
            Func<Task> action = async () => await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats"")
                VALUES ({eventId}, 'End Before Start Event', {startAt}, {endAt}, 10, 10);",
                TestContext.Current.CancellationToken);

            // Assert
            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23514"); // check_violation
            ex.Which.ConstraintName.Should().Be("CK_events_dates");
        });
    }

    [Fact]
    public async Task CheckConstraint_ShouldRejectEventUpdated_WhenEndAtModifiedBeforeStartAt()
    {
        var @event = Event.Create("Valid Dates Event", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(3), DateTimeOffset.UtcNow, 10);

        await ExecuteDbContextAsync(async db =>
        {
            db.Events.Add(@event);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        await ExecuteDbContextAsync(async db =>
        {
            var invalidEndAt = @event.StartAt.AddHours(-1); // Окончание раньше начала

            Func<Task> action = async () => await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""events""
                SET ""end_at"" = {invalidEndAt}
                WHERE ""Id"" = {@event.Id};",
                TestContext.Current.CancellationToken);

            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23514"); // check_violation
            ex.Which.ConstraintName.Should().Be("CK_events_dates");
        });
    }

    [Fact]
    public async Task CheckConstraint_ShouldAllowEvent_WhenStartAtBeforeEndAt()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // Arrange: корректный интервал дат
            var eventId = Guid.NewGuid();
            var startAt = DateTime.UtcNow.AddDays(1);
            var endAt = startAt.AddHours(2);

            // Act
            var rowsAffected = await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats"")
                VALUES ({eventId}, 'Valid Dates Event', {startAt}, {endAt}, 10, 10);",
                TestContext.Current.CancellationToken);

            // Assert
            rowsAffected.Should().Be(1);

            var inDb = await db.Events.FindAsync([eventId], TestContext.Current.CancellationToken);
            inDb.Should().NotBeNull();
            inDb.StartAt.Should().BeCloseTo(startAt, TimeSpan.FromMilliseconds(1));
            inDb.EndAt.Should().BeCloseTo(endAt, TimeSpan.FromMilliseconds(1));
        });
    }

    [Fact]
    public async Task CheckConstraint_ShouldAllowEventUpdated_WhenDatesRemainValid()
    {
        var @event = Event.Create("Reschedule Event", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), DateTimeOffset.UtcNow, 10);

        await ExecuteDbContextAsync(async db =>
        {
            db.Events.Add(@event);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        });

        await ExecuteDbContextAsync(async db =>
        {
            var newStartAt = DateTime.UtcNow.AddDays(5);
            var newEndAt = DateTime.UtcNow.AddDays(6);

            // Act: успешный перенос события на новые валидные даты
            var rowsAffected = await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ""events""
                SET ""start_at"" = {newStartAt}, ""end_at"" = {newEndAt}
                WHERE ""Id"" = {@event.Id};",
                TestContext.Current.CancellationToken);

            // Assert
            rowsAffected.Should().Be(1);
        });
    }

    #endregion

    #region PK_events

    [Fact]
    public async Task PrimaryKeyConstraint_ShouldRejectDuplicateEventId()
    {
        var existingEventId = Guid.NewGuid();

        await ExecuteDbContextAsync(async db =>
        {
            var startAt = DateTime.UtcNow.AddDays(1);
            var endAt = DateTime.UtcNow.AddDays(1).AddHours(2);

            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats"")
                VALUES ({existingEventId}, 'First Event', {startAt}, {endAt}, 10, 10);",
                TestContext.Current.CancellationToken);
        });

        await ExecuteDbContextAsync(async db =>
        {
            var startAt = DateTime.UtcNow.AddDays(2);
            var endAt = DateTime.UtcNow.AddDays(2).AddHours(2);

            // Act: попытка вставить событие с уже существующим Id
            Func<Task> action = async () => await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats"")
                VALUES ({existingEventId}, 'Duplicate ID Event', {startAt}, {endAt}, 5, 5);",
                TestContext.Current.CancellationToken);

            // Assert
            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23505"); // unique_violation
            ex.Which.ConstraintName.Should().Be("PK_events");
        });
    }

    [Fact]
    public async Task PrimaryKeyConstraint_ShouldAllowMultipleEventsWithDifferentIds()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var startAt = DateTime.UtcNow.AddDays(1);
            var endAt = DateTime.UtcNow.AddDays(1).AddHours(2);

            // Act
            var rows1 = await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats"")
                VALUES ({id1}, 'Event 1', {startAt}, {endAt}, 10, 10);",
                TestContext.Current.CancellationToken);

            var rows2 = await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats"")
                VALUES ({id2}, 'Event 2', {startAt}, {endAt}, 10, 10);",
                TestContext.Current.CancellationToken);

            // Assert
            rows1.Should().Be(1);
            rows2.Should().Be(1);
        });
    }

    #endregion

    #region NOT NULL and String Length Constraints

    [Fact]
    public async Task NotNullConstraint_ShouldRejectNullTitle()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var eventId = Guid.NewGuid();
            var startAt = DateTime.UtcNow.AddDays(1);
            var endAt = DateTime.UtcNow.AddDays(1).AddHours(2);

            // Act: title = NULL
            Func<Task> action = async () => await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats"")
                VALUES ({eventId}, NULL, {startAt}, {endAt}, 10, 10);",
                TestContext.Current.CancellationToken);

            // Assert
            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23502"); // not_null_violation
            ex.Which.ColumnName.Should().Be("title");
        });
    }

    [Fact]
    public async Task MaxLengthConstraint_ShouldRejectTitleExceeding200Chars()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var eventId = Guid.NewGuid();
            var startAt = DateTime.UtcNow.AddDays(1);
            var endAt = DateTime.UtcNow.AddDays(1).AddHours(2);
            var tooLongTitle = new string('A', 201); // 201 символ при максимуме 200

            // Act
            Func<Task> action = async () => await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats"")
                VALUES ({eventId}, {tooLongTitle}, {startAt}, {endAt}, 10, 10);",
                TestContext.Current.CancellationToken);

            // Assert
            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("22001"); // string_data_right_truncation
        });
    }

    [Fact]
    public async Task MaxLengthConstraint_ShouldAllowTitleWithExactly200Chars()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // Arrange: граничный случай ровно 200 символов
            var eventId = Guid.NewGuid();
            var startAt = DateTime.UtcNow.AddDays(1);
            var endAt = DateTime.UtcNow.AddDays(1).AddHours(2);
            var maxTitle = new string('B', 200);

            // Act
            var rowsAffected = await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats"")
                VALUES ({eventId}, {maxTitle}, {startAt}, {endAt}, 10, 10);",
                TestContext.Current.CancellationToken);

            // Assert
            rowsAffected.Should().Be(1);

            var inDb = await db.Events.FindAsync([eventId], TestContext.Current.CancellationToken);
            inDb.Should().NotBeNull();
            inDb.Title.Should().Be(maxTitle);
        });
    }

    [Fact]
    public async Task MaxLengthConstraint_ShouldRejectCurrencyExceeding3Chars()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var eventId = Guid.NewGuid();
            var startAt = DateTime.UtcNow.AddDays(1);
            var endAt = DateTime.UtcNow.AddDays(1).AddHours(2);
            var invalidCurrency = "USDD"; // 4 символа при максимуме 3

            // Act
            Func<Task> action = async () => await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats"", ""currency"")
                VALUES ({eventId}, 'Currency Test Event', {startAt}, {endAt}, 10, 10, {invalidCurrency});",
                TestContext.Current.CancellationToken);

            // Assert
            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("22001"); // string_data_right_truncation
        });
    }

    [Fact]
    public async Task MaxLengthConstraint_ShouldAllowValid3CharCurrency()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // Arrange: граничный случай ровно 3 символа
            var eventId = Guid.NewGuid();
            var startAt = DateTime.UtcNow.AddDays(1);
            var endAt = DateTime.UtcNow.AddDays(1).AddHours(2);
            var validCurrency = "EUR";

            // Act
            var rowsAffected = await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""events"" (""Id"", ""title"", ""start_at"", ""end_at"", ""total_seats"", ""available_seats"", ""currency"")
                VALUES ({eventId}, 'Currency Valid Event', {startAt}, {endAt}, 10, 10, {validCurrency});",
                TestContext.Current.CancellationToken);

            // Assert
            rowsAffected.Should().Be(1);

            var inDb = await db.Events.FindAsync([eventId], TestContext.Current.CancellationToken);
            inDb.Should().NotBeNull();
            inDb.Currency.Should().Be(validCurrency);
        });
    }

    #endregion
}
