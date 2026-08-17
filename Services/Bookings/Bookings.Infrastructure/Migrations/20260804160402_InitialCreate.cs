using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookings.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "bookings",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                event_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                seats = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_bookings", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "inbox_messages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                causation_id = table.Column<Guid>(type: "uuid", nullable: true),
                consumer_name = table.Column<string>(type: "text", nullable: false),
                topic = table.Column<string>(type: "text", nullable: false),
                partition = table.Column<int>(type: "integer", nullable: false),
                offset = table.Column<long>(type: "bigint", nullable: false),
                message_key = table.Column<string>(type: "text", nullable: false),
                message_type = table.Column<string>(type: "text", nullable: false),
                payload = table.Column<string>(type: "jsonb", nullable: false),
                headers = table.Column<string>(type: "jsonb", nullable: false),
                received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                last_error = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_inbox_messages", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "outbox_messages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                causation_id = table.Column<Guid>(type: "uuid", nullable: true),
                message_type = table.Column<string>(type: "text", nullable: false),
                topic = table.Column<string>(type: "text", nullable: false),
                message_key = table.Column<string>(type: "text", nullable: false),
                payload = table.Column<string>(type: "jsonb", nullable: false),
                headers = table.Column<string>(type: "jsonb", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                retry_count = table.Column<int>(type: "integer", nullable: false),
                next_retry_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_error = table.Column<string>(type: "text", nullable: true),
                is_dead_lettered = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_outbox_messages", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_bookings_created_at",
            table: "bookings",
            column: "created_at",
            filter: "\"status\" = 'Pending'");

        migrationBuilder.CreateIndex(
            name: "IX_bookings_user_id_status",
            table: "bookings",
            columns: new[] { "user_id", "status" });

        migrationBuilder.CreateIndex(
            name: "IX_inbox_messages_consumer_name_topic_partition_offset",
            table: "inbox_messages",
            columns: new[] { "consumer_name", "topic", "partition", "offset" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_inbox_messages_processed_at",
            table: "inbox_messages",
            column: "processed_at");

        migrationBuilder.CreateIndex(
            name: "IX_inbox_messages_received_at",
            table: "inbox_messages",
            column: "received_at");

        migrationBuilder.CreateIndex(
            name: "IX_outbox_messages_created_at_next_retry_at",
            table: "outbox_messages",
            columns: new[] { "created_at", "next_retry_at" },
            filter: "published_at IS NULL AND is_dead_lettered = false");

        migrationBuilder.CreateIndex(
            name: "IX_outbox_messages_published_at",
            table: "outbox_messages",
            column: "published_at");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "bookings");

        migrationBuilder.DropTable(
            name: "inbox_messages");

        migrationBuilder.DropTable(
            name: "outbox_messages");
    }
}
