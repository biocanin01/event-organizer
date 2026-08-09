using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventOrganizer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IntroduceEventResourceBookingAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceReservations");

            migrationBuilder.CreateTable(
                name: "EventResourceBookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventResourceBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventResourceBookings_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventResourceBookingItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventResourceBookingItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventResourceBookingItems_EventResourceBookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "EventResourceBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventResourceBookingItems_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventResourceBookingItems_BookingId_ResourceId",
                table: "EventResourceBookingItems",
                columns: new[] { "BookingId", "ResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventResourceBookingItems_ResourceId",
                table: "EventResourceBookingItems",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_EventResourceBookings_EventId",
                table: "EventResourceBookings",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventResourceBookings_Status",
                table: "EventResourceBookings",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventResourceBookingItems");

            migrationBuilder.DropTable(
                name: "EventResourceBookings");

            migrationBuilder.CreateTable(
                name: "ResourceReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceReservations_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceReservations_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceReservations_EventId",
                table: "ResourceReservations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceReservations_ResourceId",
                table: "ResourceReservations",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceReservations_Status",
                table: "ResourceReservations",
                column: "Status");
        }
    }
}
