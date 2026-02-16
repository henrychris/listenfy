using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listenfy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuildNameToGuildSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "GuildName", table: "GuildSettings", type: "text", nullable: false, defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "GuildName", table: "GuildSettings");
        }
    }
}
