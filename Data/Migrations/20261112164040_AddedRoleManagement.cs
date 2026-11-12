using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOT_Greg_v2_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedRoleManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RoleManagement",
                table: "GuildSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "RoleDiscordId",
                table: "Filters",
                type: "varchar(22)",
                maxLength: 22,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(22)",
                oldMaxLength: 22)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            // guilds that already have roles (alert role or filter roles) keep them, so their role management is enabled
            // all other guilds get the default (disabled)
            migrationBuilder.Sql(@"
                UPDATE GuildSettings s
                INNER JOIN Guilds g ON g.Id = s.GuildId
                SET s.RoleManagement = 1
                WHERE g.AlertRoleDiscordId IS NOT NULL
                   OR EXISTS (SELECT 1 FROM Filters f WHERE f.GuildId = g.Id);
            ");

            // the default filter (only matches covered by hltv) was applied in code to guilds without filters,
            // now it is a normal filter, that is added to every new guild. Existing guilds without filters get it here, so nothing changes for them
            migrationBuilder.Sql(@"
                INSERT INTO Filters (Id, GuildId, HltvCoverage, CreatedAt, UpdatedAt)
                SELECT UUID(), g.Id, 1, UTC_TIMESTAMP(6), NULL
                FROM Guilds g
                WHERE NOT EXISTS (SELECT 1 FROM Filters f WHERE f.GuildId = g.Id);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoleManagement",
                table: "GuildSettings");

            // the role is required again, so filters without role can not be kept
            // (this includes the default filters, they are applied in code again)
            migrationBuilder.Sql(@"
                DELETE FROM Filters
                WHERE RoleDiscordId IS NULL;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "RoleDiscordId",
                table: "Filters",
                type: "varchar(22)",
                maxLength: 22,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(22)",
                oldMaxLength: 22,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
