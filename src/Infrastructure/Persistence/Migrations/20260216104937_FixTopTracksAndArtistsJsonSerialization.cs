using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listenfy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixTopTracksAndArtistsJsonSerialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TopTracks",
                table: "WeeklyStats",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb"
            );

            migrationBuilder.AlterColumn<string>(
                name: "TopArtists",
                table: "WeeklyStats",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TopTracks",
                table: "WeeklyStats",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "TopArtists",
                table: "WeeklyStats",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true
            );
        }
    }
}
