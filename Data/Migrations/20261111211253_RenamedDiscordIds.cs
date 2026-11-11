using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOT_Greg_v2_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenamedDiscordIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LinkedRoleId",
                table: "RoleLinks",
                newName: "LinkedRoleDiscordId");

            migrationBuilder.RenameColumn(
                name: "BotRoleId",
                table: "RoleLinks",
                newName: "BotRoleDiscordId");

            migrationBuilder.RenameColumn(
                name: "ResultAlertsChannelId",
                table: "Guilds",
                newName: "ResultAlertsChannelDiscordId");

            migrationBuilder.RenameColumn(
                name: "MatchAlertsChannelId",
                table: "Guilds",
                newName: "MatchAlertsChannelDiscordId");

            migrationBuilder.RenameColumn(
                name: "GuildId",
                table: "Guilds",
                newName: "DiscordId");

            migrationBuilder.RenameColumn(
                name: "AlertRoleId",
                table: "Guilds",
                newName: "AlertRoleDiscordId");

            migrationBuilder.RenameIndex(
                name: "IX_Guilds_GuildId",
                table: "Guilds",
                newName: "IX_Guilds_DiscordId");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "Filters",
                newName: "RoleDiscordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LinkedRoleDiscordId",
                table: "RoleLinks",
                newName: "LinkedRoleId");

            migrationBuilder.RenameColumn(
                name: "BotRoleDiscordId",
                table: "RoleLinks",
                newName: "BotRoleId");

            migrationBuilder.RenameColumn(
                name: "ResultAlertsChannelDiscordId",
                table: "Guilds",
                newName: "ResultAlertsChannelId");

            migrationBuilder.RenameColumn(
                name: "MatchAlertsChannelDiscordId",
                table: "Guilds",
                newName: "MatchAlertsChannelId");

            migrationBuilder.RenameColumn(
                name: "DiscordId",
                table: "Guilds",
                newName: "GuildId");

            migrationBuilder.RenameColumn(
                name: "AlertRoleDiscordId",
                table: "Guilds",
                newName: "AlertRoleId");

            migrationBuilder.RenameIndex(
                name: "IX_Guilds_DiscordId",
                table: "Guilds",
                newName: "IX_Guilds_GuildId");

            migrationBuilder.RenameColumn(
                name: "RoleDiscordId",
                table: "Filters",
                newName: "RoleId");
        }
    }
}
