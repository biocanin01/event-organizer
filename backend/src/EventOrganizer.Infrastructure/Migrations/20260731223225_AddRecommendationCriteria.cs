using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventOrganizer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendationCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Area",
                table: "Resources",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Resources",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "Resources",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "QualityScore",
                table: "Resources",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<string>(
                name: "Area",
                table: "Events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "General");

            migrationBuilder.AddColumn<decimal>(
                name: "Budget",
                table: "Events",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 1000m);

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Area",
                table: "Resources",
                column: "Area");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Area",
                table: "Events",
                column: "Area");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Resources_Area",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Events_Area",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "QualityScore",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Budget",
                table: "Events");
        }
    }
}
