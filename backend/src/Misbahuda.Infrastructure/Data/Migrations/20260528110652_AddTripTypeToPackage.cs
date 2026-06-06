using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misbahuda.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTripTypeToPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RouteDetail",
                table: "TourPackages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TripDuration",
                table: "TourPackages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TripRoute",
                table: "TourPackages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RouteDetail",
                table: "TourPackages");

            migrationBuilder.DropColumn(
                name: "TripDuration",
                table: "TourPackages");

            migrationBuilder.DropColumn(
                name: "TripRoute",
                table: "TourPackages");
        }
    }
}
