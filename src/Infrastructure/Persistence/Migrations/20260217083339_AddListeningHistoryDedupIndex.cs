using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listenfy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListeningHistoryDedupIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_ListeningHistories_SpotifyUserId", table: "ListeningHistories");

            migrationBuilder.CreateIndex(
                name: "IX_ListeningHistories_SpotifyUserId_TrackId_PlayedAt",
                table: "ListeningHistories",
                columns: new[] { "SpotifyUserId", "TrackId", "PlayedAt" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_ListeningHistories_SpotifyUserId_TrackId_PlayedAt", table: "ListeningHistories");

            migrationBuilder.CreateIndex(name: "IX_ListeningHistories_SpotifyUserId", table: "ListeningHistories", column: "SpotifyUserId");
        }
    }
}
