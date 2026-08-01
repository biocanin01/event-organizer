using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventOrganizer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequiredSpeakerCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequiredSpeakerCount",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiredSpeakerCount",
                table: "Events");
        }
    }
}
