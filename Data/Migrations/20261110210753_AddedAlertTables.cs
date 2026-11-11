using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOT_Greg_v2_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedAlertTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlertRoleId",
                table: "Guilds",
                type: "varchar(22)",
                maxLength: 22,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

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

            migrationBuilder.AddColumn<string>(
                name: "MatchAlertsChannelId",
                table: "Guilds",
                type: "varchar(22)",
                maxLength: 22,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

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
                defaultValue: true); // already existing guilds get the same default as new guilds

            migrationBuilder.AddColumn<string>(
                name: "ResultAlertsChannelId",
                table: "Guilds",
                type: "varchar(22)",
                maxLength: 22,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Filters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Team = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Event = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EventTier = table.Column<int>(type: "int", nullable: true),
                    VrsRank = table.Column<int>(type: "int", nullable: true),
                    HltvCoverage = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    RoleId = table.Column<string>(type: "varchar(22)", maxLength: 22, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GuildId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Filters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Filters_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HistoryMatchAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MatchId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AlertsSent = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoryMatchAlerts", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HistoryResultAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MatchId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlertsSent = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoryResultAlerts", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RoleLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BotRoleId = table.Column<string>(type: "varchar(22)", maxLength: 22, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LinkedRoleId = table.Column<string>(type: "varchar(22)", maxLength: 22, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GuildId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleLinks_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Filters_GuildId",
                table: "Filters",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoryMatchAlerts_MatchId",
                table: "HistoryMatchAlerts",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistoryResultAlerts_MatchId",
                table: "HistoryResultAlerts",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleLinks_GuildId",
                table: "RoleLinks",
                column: "GuildId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Filters");

            migrationBuilder.DropTable(
                name: "HistoryMatchAlerts");

            migrationBuilder.DropTable(
                name: "HistoryResultAlerts");

            migrationBuilder.DropTable(
                name: "RoleLinks");

            migrationBuilder.DropColumn(
                name: "AlertRoleId",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "AlertsPaused",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "CensorResults",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "MatchAlertsChannelId",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "MentionOnDelays",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "MentionOnResults",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "ResultAlertsChannelId",
                table: "Guilds");
        }
    }
}
