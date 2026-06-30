using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreEvents.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexAndCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_events_start_at",
                table: "events");

            migrationBuilder.DropIndex(
                name: "IX_events_title",
                table: "events");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_events_start_at_end_at",
                table: "events",
                columns: new[] { "start_at", "end_at" },
                descending: new[] { true, false });

            migrationBuilder.CreateIndex(
                name: "IX_events_title",
                table: "events",
                column: "title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_events_dates",
                table: "events",
                sql: "\"start_at\" < \"end_at\"");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_created_at",
                table: "bookings",
                column: "created_at",
                filter: "\"status\" = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_events_start_at_end_at",
                table: "events");

            migrationBuilder.DropIndex(
                name: "IX_events_title",
                table: "events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_events_dates",
                table: "events");

            migrationBuilder.DropIndex(
                name: "IX_bookings_created_at",
                table: "bookings");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_events_start_at",
                table: "events",
                column: "start_at");

            migrationBuilder.CreateIndex(
                name: "IX_events_title",
                table: "events",
                column: "title");
        }
    }
}
