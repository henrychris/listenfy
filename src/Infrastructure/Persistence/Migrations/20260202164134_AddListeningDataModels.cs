using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listenfy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListeningDataModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListeningHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TrackId = table.Column<string>(type: "text", nullable: false),
                    TrackName = table.Column<string>(type: "text", nullable: false),
                    ArtistName = table.Column<string>(type: "text", nullable: false),
                    AlbumName = table.Column<string>(type: "text", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: false),
                    PlayedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContextType = table.Column<string>(type: "text", nullable: true),
                    ContextUri = table.Column<string>(type: "text", nullable: true),
                    SpotifyUserId = table.Column<string>(type: "text", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListeningHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListeningHistories_SpotifyUsers_SpotifyUserId",
                        column: x => x.SpotifyUserId,
                        principalTable: "SpotifyUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "SpotifyFetchMetadata",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    LastFetchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastPlayedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TracksFetchedInLastRun = table.Column<int>(type: "integer", nullable: false),
                    SpotifyUserId = table.Column<string>(type: "text", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpotifyFetchMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpotifyFetchMetadata_SpotifyUsers_SpotifyUserId",
                        column: x => x.SpotifyUserId,
                        principalTable: "SpotifyUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "WeeklyStats",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    WeekIdentifier = table.Column<string>(type: "text", nullable: false),
                    WeekStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WeekEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalMinutesListened = table.Column<int>(type: "integer", nullable: false),
                    TotalTracksPlayed = table.Column<int>(type: "integer", nullable: false),
                    UniqueTracksPlayed = table.Column<int>(type: "integer", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SpotifyUserId = table.Column<string>(type: "text", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TopArtists = table.Column<string>(type: "jsonb", nullable: false),
                    TopTracks = table.Column<string>(type: "jsonb", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyStats_SpotifyUsers_SpotifyUserId",
                        column: x => x.SpotifyUserId,
                        principalTable: "SpotifyUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(name: "IX_ListeningHistories_SpotifyUserId", table: "ListeningHistories", column: "SpotifyUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotifyFetchMetadata_SpotifyUserId",
                table: "SpotifyFetchMetadata",
                column: "SpotifyUserId",
                unique: true
            );

            migrationBuilder.CreateIndex(name: "IX_WeeklyStats_SpotifyUserId", table: "WeeklyStats", column: "SpotifyUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ListeningHistories");

            migrationBuilder.DropTable(name: "SpotifyFetchMetadata");

            migrationBuilder.DropTable(name: "WeeklyStats");
        }
    }
}
