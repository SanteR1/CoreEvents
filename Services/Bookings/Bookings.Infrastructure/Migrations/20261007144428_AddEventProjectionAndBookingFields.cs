using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookings.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddEventProjectionAndBookingFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "status",
            table: "bookings",
            type: "character varying(30)",
            maxLength: 30,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(20)",
            oldMaxLength: 20);

        migrationBuilder.AddColumn<string>(
            name: "cancellation_reason",
            table: "bookings",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "cancellation_requested_at",
            table: "bookings",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "currency",
            table: "bookings",
            type: "character varying(3)",
            maxLength: 3,
            nullable: false,
            defaultValue: "KZT");

        migrationBuilder.AddColumn<decimal>(
            name: "discount_amount",
            table: "bookings",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0.00m);

        migrationBuilder.AddColumn<string>(
            name: "rejection_reason",
            table: "bookings",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "total_price",
            table: "bookings",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0.00m);

        migrationBuilder.AddColumn<decimal>(
            name: "unit_price",
            table: "bookings",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0.00m);

        migrationBuilder.CreateTable(
            name: "event_projections",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                end_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "KZT"),
                price_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                total_seats = table.Column<int>(type: "integer", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_event_projections", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_bookings_event_id_status",
            table: "bookings",
            columns: new[] { "event_id", "status" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "event_projections");

        migrationBuilder.DropIndex(
            name: "IX_bookings_event_id_status",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "cancellation_reason",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "cancellation_requested_at",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "currency",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "discount_amount",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "rejection_reason",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "total_price",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "unit_price",
            table: "bookings");

        migrationBuilder.AlterColumn<string>(
            name: "status",
            table: "bookings",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(30)",
            oldMaxLength: 30);
    }
}
