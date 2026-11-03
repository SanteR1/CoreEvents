using AwesomeAssertions;
using Events.IntegrationTests.Infrastructure.Bases;
using Events.IntegrationTests.Infrastructure.Collections;
using Events.IntegrationTests.Infrastructure.Factories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Events.IntegrationTests.Database;

[Collection(TestCollections.ApiOnly)]
public class SeatReservationsDatabaseConstraintsTests(ApiOnlyIntegrationTestFactory factory)
    : IntegrationTestBase<ApiOnlyIntegrationTestFactory>(factory)
{
    #region chk_reservations_price_math

    [Fact]
    public async Task CheckConstraint_ShouldRejectReservation_WhenTotalPriceDoesNotMatchFormula()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // Arrange: seats * unit_price - discount = 2 * 100 - 0 = 200, но total_price = 150
            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            int seats = 2;
            decimal unitPrice = 100m;
            decimal discount = 0m;
            decimal invalidTotalPrice = 150m;
            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.AddMinutes(15);

            Func<Task> action = async () => await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""event_seat_reservations""
                (""booking_id"", ""event_id"", ""seats"", ""unit_price"", ""total_price"", ""currency"", ""discount_amount"", ""price_version"", ""status"", ""created_at"", ""expires_at"", ""correlation_id"", ""causation_id"")
                VALUES ({bookingId}, {eventId}, {seats}, {unitPrice}, {invalidTotalPrice}, 'KZT', {discount}, 1, 'Reserved', {now}, {expiresAt}, {bookingId}, {bookingId});",
                TestContext.Current.CancellationToken);

            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23514"); // check_violation
            ex.Which.ConstraintName.Should().Be("chk_reservations_price_math");
        });
    }

    [Fact]
    public async Task CheckConstraint_ShouldAllowReservation_WhenTotalPriceMatchesFormula()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // Arrange: 3 * 100 - 20 = 280
            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            int seats = 3;
            decimal unitPrice = 100m;
            decimal discount = 20m;
            decimal validTotalPrice = 280m;
            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.AddMinutes(15);

            var rowsAffected = await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""event_seat_reservations""
                (""booking_id"", ""event_id"", ""seats"", ""unit_price"", ""total_price"", ""currency"", ""discount_amount"", ""price_version"", ""status"", ""created_at"", ""expires_at"", ""correlation_id"", ""causation_id"")
                VALUES ({bookingId}, {eventId}, {seats}, {unitPrice}, {validTotalPrice}, 'KZT', {discount}, 1, 'Reserved', {now}, {expiresAt}, {bookingId}, {bookingId});",
                TestContext.Current.CancellationToken);

            rowsAffected.Should().Be(1);
        });
    }

    #endregion

    #region chk_reservations_reserved_has_expiration

    [Fact]
    public async Task CheckConstraint_ShouldRejectReservedStatus_WhenExpiresAtIsNull()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // Arrange: status = 'Reserved', но expires_at IS NULL
            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            Func<Task> action = async () => await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""event_seat_reservations""
                (""booking_id"", ""event_id"", ""seats"", ""unit_price"", ""total_price"", ""currency"", ""discount_amount"", ""price_version"", ""status"", ""created_at"", ""expires_at"", ""correlation_id"", ""causation_id"")
                VALUES ({bookingId}, {eventId}, 1, 100, 100, 'KZT', 0, 1, 'Reserved', {now}, NULL, {bookingId}, {bookingId});",
                TestContext.Current.CancellationToken);

            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23514"); // check_violation
            ex.Which.ConstraintName.Should().Be("chk_reservations_reserved_has_expiration");
        });
    }

    [Fact]
    public async Task CheckConstraint_ShouldAllowConfirmedStatus_WhenExpiresAtIsNull()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // Arrange: статус 'Confirmed' не требует expires_at
            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            var rowsAffected = await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""event_seat_reservations""
                (""booking_id"", ""event_id"", ""seats"", ""unit_price"", ""total_price"", ""currency"", ""discount_amount"", ""price_version"", ""status"", ""created_at"", ""expires_at"", ""correlation_id"", ""causation_id"")
                VALUES ({bookingId}, {eventId}, 1, 100, 100, 'KZT', 0, 1, 'Confirmed', {now}, NULL, {bookingId}, {bookingId});",
                TestContext.Current.CancellationToken);

            rowsAffected.Should().Be(1);
        });
    }

    #endregion

    #region chk_reservations_rejected_has_reason

    [Fact]
    public async Task CheckConstraint_ShouldRejectRejectedStatus_WhenRejectionReasonIsNull()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // Arrange: status = 'Rejected', но rejection_reason IS NULL
            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            Func<Task> action = async () => await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""event_seat_reservations""
                (""booking_id"", ""event_id"", ""seats"", ""unit_price"", ""total_price"", ""currency"", ""discount_amount"", ""price_version"", ""status"", ""rejection_reason"", ""created_at"", ""correlation_id"", ""causation_id"")
                VALUES ({bookingId}, {eventId}, 1, 0, 0, 'KZT', 0, 1, 'Rejected', NULL, {now}, {bookingId}, {bookingId});",
                TestContext.Current.CancellationToken);

            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23514"); // check_violation
            ex.Which.ConstraintName.Should().Be("chk_reservations_rejected_has_reason");
        });
    }

    [Fact]
    public async Task CheckConstraint_ShouldAllowRejectedStatus_WhenRejectionReasonIsProvided()
    {
        await ExecuteDbContextAsync(async db =>
        {
            // Arrange: status = 'Rejected' с указанием причины
            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            var rowsAffected = await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""event_seat_reservations""
                (""booking_id"", ""event_id"", ""seats"", ""unit_price"", ""total_price"", ""currency"", ""discount_amount"", ""price_version"", ""status"", ""rejection_reason"", ""created_at"", ""correlation_id"", ""causation_id"")
                VALUES ({bookingId}, {eventId}, 1, 0, 0, 'KZT', 0, 1, 'Rejected', 'SeatsNotAvailable', {now}, {bookingId}, {bookingId});",
                TestContext.Current.CancellationToken);

            rowsAffected.Should().Be(1);
        });
    }

    #endregion

    #region chk_reservations_seats_positive

    [Fact]
    public async Task CheckConstraint_ShouldRejectZeroOrNegativeSeats()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.AddMinutes(15);

            // Act: seats = 0
            Func<Task> action = async () => await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""event_seat_reservations""
                (""booking_id"", ""event_id"", ""seats"", ""unit_price"", ""total_price"", ""currency"", ""discount_amount"", ""price_version"", ""status"", ""created_at"", ""expires_at"", ""correlation_id"", ""causation_id"")
                VALUES ({bookingId}, {eventId}, 0, 100, 0, 'KZT', 0, 1, 'Reserved', {now}, {expiresAt}, {bookingId}, {bookingId});",
                TestContext.Current.CancellationToken);

            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23514"); // check_violation
            ex.Which.ConstraintName.Should().Be("chk_reservations_seats_positive");
        });
    }

    #endregion

    #region chk_reservations_discount_non_negative

    [Fact]
    public async Task CheckConstraint_ShouldRejectNegativeDiscount()
    {
        await ExecuteDbContextAsync(async db =>
        {
            var bookingId = Guid.NewGuid();
            var eventId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.AddMinutes(15);
            decimal negativeDiscount = -10m;

            // Act: discount_amount < 0 (total_price = 1 * 100 - (-10) = 110)
            Func<Task> action = async () => await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""event_seat_reservations""
                (""booking_id"", ""event_id"", ""seats"", ""unit_price"", ""total_price"", ""currency"", ""discount_amount"", ""price_version"", ""status"", ""created_at"", ""expires_at"", ""correlation_id"", ""causation_id"")
                VALUES ({bookingId}, {eventId}, 1, 100, 110, 'KZT', {negativeDiscount}, 1, 'Reserved', {now}, {expiresAt}, {bookingId}, {bookingId});",
                TestContext.Current.CancellationToken);

            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23514"); // check_violation
            ex.Which.ConstraintName.Should().Be("chk_reservations_discount_non_negative");
        });
    }

    #endregion

    #region PK_event_seat_reservations

    [Fact]
    public async Task PrimaryKeyConstraint_ShouldRejectDuplicateBookingId()
    {
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(15);

        await ExecuteDbContextAsync(async db =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""event_seat_reservations""
                (""booking_id"", ""event_id"", ""seats"", ""unit_price"", ""total_price"", ""currency"", ""discount_amount"", ""price_version"", ""status"", ""created_at"", ""expires_at"", ""correlation_id"", ""causation_id"")
                VALUES ({bookingId}, {eventId}, 1, 100, 100, 'KZT', 0, 1, 'Reserved', {now}, {expiresAt}, {bookingId}, {bookingId});",
                TestContext.Current.CancellationToken);
        });

        await ExecuteDbContextAsync(async db =>
        {
            // Act: повторная вставка с тем же booking_id
            Func<Task> action = async () => await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""event_seat_reservations""
                (""booking_id"", ""event_id"", ""seats"", ""unit_price"", ""total_price"", ""currency"", ""discount_amount"", ""price_version"", ""status"", ""created_at"", ""expires_at"", ""correlation_id"", ""causation_id"")
                VALUES ({bookingId}, {eventId}, 2, 100, 200, 'KZT', 0, 1, 'Reserved', {now}, {expiresAt}, {bookingId}, {bookingId});",
                TestContext.Current.CancellationToken);

            var ex = await action.Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be("23505"); // unique_violation
            ex.Which.ConstraintName.Should().Be("PK_event_seat_reservations");
        });
    }

    #endregion
}

