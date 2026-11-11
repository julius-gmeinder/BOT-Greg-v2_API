using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOT_Greg_v2_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class MovedSettingsToGuildSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AlertsPaused = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MentionOnDelays = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MentionOnResults = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CensorResults = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    GuildId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildSettings_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_GuildSettings_GuildId",
                table: "GuildSettings",
                column: "GuildId",
                unique: true);

            // copies the settings of the existing guilds into the new table before the old columns are dropped
            migrationBuilder.Sql(@"
                INSERT INTO GuildSettings (Id, GuildId, AlertsPaused, MentionOnDelays, MentionOnResults, CensorResults, CreatedAt, UpdatedAt)
                SELECT UUID(), Id, AlertsPaused, MentionOnDelays, MentionOnResults, CensorResults, UTC_TIMESTAMP(6), NULL
                FROM Guilds;
            ");

            migrationBuilder.DropColumn(
                name: "AlertsPaused",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "CensorResults",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "MentionOnDelays",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "MentionOnResults",
                table: "Guilds");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AlertsPaused",
                table: "Guilds",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CensorResults",
                table: "Guilds",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MentionOnDelays",
                table: "Guilds",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MentionOnResults",
                table: "Guilds",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true); // same default as new guilds

            // copies the settings back into the guilds before the table is dropped
            migrationBuilder.Sql(@"
                UPDATE Guilds g
                INNER JOIN GuildSettings s ON s.GuildId = g.Id
                SET g.AlertsPaused = s.AlertsPaused,
                    g.MentionOnDelays = s.MentionOnDelays,
                    g.MentionOnResults = s.MentionOnResults,
                    g.CensorResults = s.CensorResults;
            ");

            migrationBuilder.DropTable(
                name: "GuildSettings");
        }
    }
}
