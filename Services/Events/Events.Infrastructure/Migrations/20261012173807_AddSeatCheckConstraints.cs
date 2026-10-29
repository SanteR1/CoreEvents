using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Events.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddSeatCheckConstraints : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCheckConstraint(
            name: "CK_events_available_seats_le_total",
            table: "events",
            sql: "\"available_seats\" <= \"total_seats\"");

        migrationBuilder.AddCheckConstraint(
            name: "CK_events_available_seats_non_negative",
            table: "events",
            sql: "\"available_seats\" >= 0");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_events_available_seats_le_total",
            table: "events");

        migrationBuilder.DropCheckConstraint(
            name: "CK_events_available_seats_non_negative",
            table: "events");
    }
}
