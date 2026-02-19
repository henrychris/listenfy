using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listenfy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClientIdToSpotifyUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "ClientId", table: "SpotifyUsers", type: "text", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ClientId", table: "SpotifyUsers");
        }
    }
}
