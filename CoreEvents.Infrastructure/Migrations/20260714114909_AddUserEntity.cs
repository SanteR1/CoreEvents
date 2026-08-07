using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreEvents.Migrations;

/// <inheritdoc />
public partial class AddUserEntity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "user_id",
            table: "bookings",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        // На момент создания таблица booking уже существовала и по этому EF сгенерировал defaultValue - удаляем
        migrationBuilder.Sql("ALTER TABLE bookings ALTER COLUMN user_id DROP DEFAULT;");

        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                passwordhash = table.Column<string>(type: "text", nullable: false),
                role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_users", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_bookings_user_id",
            table: "bookings",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_users_user",
            table: "users",
            column: "user",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_bookings_users_user_id",
            table: "bookings",
            column: "user_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_bookings_users_user_id",
            table: "bookings");

        migrationBuilder.DropTable(
            name: "users");

        migrationBuilder.DropIndex(
            name: "IX_bookings_user_id",
            table: "bookings");

        migrationBuilder.DropColumn(
            name: "user_id",
            table: "bookings");
    }
}
