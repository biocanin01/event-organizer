using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventOrganizer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingDraftSubmissionWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresEquipment",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "HoldExpiresAtUtc",
                table: "EventResourceBookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAtUtc",
                table: "EventResourceBookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                INSERT INTO "EventResourceBookings"
                    ("Id", "EventId", "Status", "Version", "CreatedAtUtc", "UpdatedAtUtc", "SubmittedAtUtc", "HoldExpiresAtUtc")
                SELECT
                    md5(e."Id"::text || ':booking')::uuid,
                    e."Id",
                    'Draft',
                    1,
                    e."CreatedAtUtc",
                    NULL,
                    NULL,
                    NULL
                FROM "Events" e
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "EventResourceBookings" b
                    WHERE b."EventId" = e."Id"
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresEquipment",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "HoldExpiresAtUtc",
                table: "EventResourceBookings");

            migrationBuilder.DropColumn(
                name: "SubmittedAtUtc",
                table: "EventResourceBookings");
        }
    }
}
