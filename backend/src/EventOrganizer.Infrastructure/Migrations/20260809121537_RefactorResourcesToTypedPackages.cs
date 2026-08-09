using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventOrganizer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorResourcesToTypedPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"ResourceReservations\";");
            migrationBuilder.Sql("DELETE FROM \"Resources\" WHERE \"Type\" IN ('Equipment', 'TechnicalSupport');");

            migrationBuilder.DropIndex(
                name: "IX_Resources_Area",
                table: "Resources");

            migrationBuilder.RenameColumn(
                name: "Area",
                table: "Resources",
                newName: "ExpertiseArea");

            migrationBuilder.AlterColumn<string>(
                name: "ExpertiseArea",
                table: "Resources",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.Sql("UPDATE \"Resources\" SET \"ExpertiseArea\" = NULL WHERE \"Type\" <> 'Speaker';");

            migrationBuilder.AddColumn<string>(
                name: "ContentsSummary",
                table: "Resources",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludesTechnicalSupport",
                table: "Resources",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderName",
                table: "Resources",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceArea",
                table: "Resources",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupportedCapacity",
                table: "Resources",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Resources",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Resources_ServiceArea",
                table: "Resources",
                column: "ServiceArea");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_SupportedCapacity",
                table: "Resources",
                column: "SupportedCapacity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Resources_ServiceArea",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Resources_SupportedCapacity",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ContentsSummary",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "IncludesTechnicalSupport",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ProviderName",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ServiceArea",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "SupportedCapacity",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Resources");

            migrationBuilder.RenameColumn(
                name: "ExpertiseArea",
                table: "Resources",
                newName: "Area");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Area",
                table: "Resources",
                column: "Area");
        }
    }
}
