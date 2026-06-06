using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misbahuda.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAirportTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArrivalAirport",
                table: "Pilgrims",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArrivalTime",
                table: "Pilgrims",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepartureAirport",
                table: "Pilgrims",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepartureTime",
                table: "Pilgrims",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AirportTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PilgrimId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DriverName = table.Column<string>(type: "text", nullable: true),
                    DriverPhone = table.Column<string>(type: "text", nullable: true),
                    VehicleNumber = table.Column<string>(type: "text", nullable: true),
                    VehicleType = table.Column<string>(type: "text", nullable: true),
                    MeetingPoint = table.Column<string>(type: "text", nullable: true),
                    ScheduledTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AirportTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AirportTransfers_Pilgrims_PilgrimId",
                        column: x => x.PilgrimId,
                        principalTable: "Pilgrims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AirportTransfers_PilgrimId",
                table: "AirportTransfers",
                column: "PilgrimId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AirportTransfers");

            migrationBuilder.DropColumn(
                name: "ArrivalAirport",
                table: "Pilgrims");

            migrationBuilder.DropColumn(
                name: "ArrivalTime",
                table: "Pilgrims");

            migrationBuilder.DropColumn(
                name: "DepartureAirport",
                table: "Pilgrims");

            migrationBuilder.DropColumn(
                name: "DepartureTime",
                table: "Pilgrims");
        }
    }
}
