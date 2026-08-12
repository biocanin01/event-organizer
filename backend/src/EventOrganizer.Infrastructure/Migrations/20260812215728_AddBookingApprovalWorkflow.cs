using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventOrganizer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DecidedAtUtc",
                table: "EventResourceBookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DecidedByUserId",
                table: "EventResourceBookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionReason",
                table: "EventResourceBookings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DecidedAtUtc",
                table: "EventResourceBookings");

            migrationBuilder.DropColumn(
                name: "DecidedByUserId",
                table: "EventResourceBookings");

            migrationBuilder.DropColumn(
                name: "DecisionReason",
                table: "EventResourceBookings");
        }
    }
}
