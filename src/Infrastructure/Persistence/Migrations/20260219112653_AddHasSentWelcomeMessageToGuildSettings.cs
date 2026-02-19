using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Listenfy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHasSentWelcomeMessageToGuildSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasSentWelcomeMessage",
                table: "GuildSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "HasSentWelcomeMessage", table: "GuildSettings");
        }
    }
}
