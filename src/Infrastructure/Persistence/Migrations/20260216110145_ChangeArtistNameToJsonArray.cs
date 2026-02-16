using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listenfy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeArtistNameToJsonArray : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ArtistName", table: "ListeningHistories");

            migrationBuilder.AddColumn<string>(name: "ArtistNames", table: "ListeningHistories", type: "jsonb", nullable: false, defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ArtistNames", table: "ListeningHistories");

            migrationBuilder.AddColumn<string>(name: "ArtistName", table: "ListeningHistories", type: "text", nullable: false, defaultValue: "");
        }
    }
}
