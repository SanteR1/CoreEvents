using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Events.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddSeatReservationsAndEventPricing : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "currency",
            table: "events",
            type: "character varying(3)",
            maxLength: 3,
            nullable: false,
            defaultValue: "KZT");

        migrationBuilder.AddColumn<bool>(
            name: "is_active",
            table: "events",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<decimal>(
            name: "price",
            table: "events",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0.00m);

        migrationBuilder.AddColumn<long>(
            name: "price_version",
            table: "events",
            type: "bigint",
            nullable: false,
            defaultValue: 1L);

        migrationBuilder.AddColumn<long>(
            name: "version",
            table: "events",
            type: "bigint",
            nullable: false,
            defaultValue: 1L);

        migrationBuilder.CreateTable(
            name: "event_seat_reservations",
            columns: table => new
            {
                booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                event_id = table.Column<Guid>(type: "uuid", nullable: false),
                seats = table.Column<int>(type: "integer", nullable: false),
                unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                total_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                price_version = table.Column<long>(type: "bigint", nullable: false),
                status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                rejection_reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                causation_id = table.Column<Guid>(type: "uuid", nullable: false),
                xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_event_seat_reservations", x => x.booking_id);
                table.CheckConstraint("chk_reservations_discount_non_negative", "\"discount_amount\" >= 0");
                table.CheckConstraint("chk_reservations_price_math", "\"total_price\" = (\"seats\" * \"unit_price\") - \"discount_amount\"");
                table.CheckConstraint("chk_reservations_rejected_has_reason", "\"status\" != 'Rejected' OR \"rejection_reason\" IS NOT NULL");
                table.CheckConstraint("chk_reservations_reserved_has_expiration", "\"status\" != 'Reserved' OR \"expires_at\" IS NOT NULL");
                table.CheckConstraint("chk_reservations_seats_positive", "\"seats\" > 0");
                table.CheckConstraint("chk_reservations_total_price_non_negative", "\"total_price\" >= 0");
                table.CheckConstraint("chk_reservations_unit_price_non_negative", "\"unit_price\" >= 0");
            });

        migrationBuilder.CreateIndex(
            name: "ix_seat_reservations_correlation_id",
            table: "event_seat_reservations",
            column: "correlation_id");

        migrationBuilder.CreateIndex(
            name: "ix_seat_reservations_event_status",
            table: "event_seat_reservations",
            columns: new[] { "event_id", "status" });

        migrationBuilder.CreateIndex(
            name: "ix_seat_reservations_expires_at",
            table: "event_seat_reservations",
            column: "expires_at",
            filter: "\"status\" = 'Reserved'");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "event_seat_reservations");

        migrationBuilder.DropColumn(
            name: "currency",
            table: "events");

        migrationBuilder.DropColumn(
            name: "is_active",
            table: "events");

        migrationBuilder.DropColumn(
            name: "price",
            table: "events");

        migrationBuilder.DropColumn(
            name: "price_version",
            table: "events");

        migrationBuilder.DropColumn(
            name: "version",
            table: "events");
    }
}
