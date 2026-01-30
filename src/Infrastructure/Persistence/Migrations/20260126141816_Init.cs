using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listenfy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DiscordGuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    StatsChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WeeklySummaryDay = table.Column<int>(type: "integer", nullable: false),
                    WeeklySummaryTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildSettings", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "SpotifyUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    SpotifyUserId = table.Column<string>(type: "text", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    TokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpotifyUsers", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "UserConnections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DiscordUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ConnectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OAuthState = table.Column<string>(type: "text", nullable: true),
                    GuildId = table.Column<string>(type: "text", nullable: false),
                    SpotifyUserId = table.Column<string>(type: "text", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserConnections_GuildSettings_GuildId",
                        column: x => x.GuildId,
                        principalTable: "GuildSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_UserConnections_SpotifyUsers_SpotifyUserId",
                        column: x => x.SpotifyUserId,
                        principalTable: "SpotifyUsers",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateIndex(name: "IX_GuildSettings_DiscordGuildId", table: "GuildSettings", column: "DiscordGuildId", unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserConnections_GuildId_DiscordUserId",
                table: "UserConnections",
                columns: new[] { "GuildId", "DiscordUserId" },
                unique: true
            );

            migrationBuilder.CreateIndex(name: "IX_UserConnections_SpotifyUserId", table: "UserConnections", column: "SpotifyUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "UserConnections");

            migrationBuilder.DropTable(name: "GuildSettings");

            migrationBuilder.DropTable(name: "SpotifyUsers");
        }
    }
}
