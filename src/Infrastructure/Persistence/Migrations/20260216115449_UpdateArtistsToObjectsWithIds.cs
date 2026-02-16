using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listenfy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateArtistsToObjectsWithIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ArtistNames", table: "ListeningHistories");

            migrationBuilder.AddColumn<string>(name: "Artists", table: "ListeningHistories", type: "jsonb", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Artists", table: "ListeningHistories");

            migrationBuilder.AddColumn<string>(name: "ArtistNames", table: "ListeningHistories", type: "jsonb", nullable: false, defaultValue: "");
        }
    }
}
