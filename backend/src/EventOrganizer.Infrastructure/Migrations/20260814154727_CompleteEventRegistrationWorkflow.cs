using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventOrganizer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteEventRegistrationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DecidedAtUtc",
                table: "Registrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DecidedByUserId",
                table: "Registrations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Registrations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Registrations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_DecidedByUserId",
                table: "Registrations",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_Status",
                table: "Registrations",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_Registrations_AspNetUsers_DecidedByUserId",
                table: "Registrations",
                column: "DecidedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Registrations_AspNetUsers_DecidedByUserId",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_DecidedByUserId",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_Status",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "DecidedAtUtc",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "DecidedByUserId",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Registrations");
        }
    }
}
