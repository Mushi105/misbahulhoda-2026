using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misbahuda.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceTripRouteWithDestinations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TripRoute",
                table: "TourPackages");

            migrationBuilder.AddColumn<string>(
                name: "Destinations",
                table: "TourPackages",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Destinations",
                table: "TourPackages");

            migrationBuilder.AddColumn<int>(
                name: "TripRoute",
                table: "TourPackages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
