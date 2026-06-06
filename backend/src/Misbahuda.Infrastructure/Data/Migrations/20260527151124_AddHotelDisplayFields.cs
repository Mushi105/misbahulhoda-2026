using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misbahuda.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelDisplayFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdhanAsr",
                table: "Hotels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdhanFajr",
                table: "Hotels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdhanIsha",
                table: "Hotels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdhanMaghrib",
                table: "Hotels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdhanZuhr",
                table: "Hotels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Amenities",
                table: "Hotels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorClass",
                table: "Hotels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HaramDistanceText",
                table: "Hotels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HaramLatitude",
                table: "Hotels",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HaramLongitude",
                table: "Hotels",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IconEmoji",
                table: "Hotels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JamatAsr",
                table: "Hotels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JamatFajr",
                table: "Hotels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JamatIsha",
                table: "Hotels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JamatMaghrib",
                table: "Hotels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JamatZuhr",
                table: "Hotels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NearHaram",
                table: "Hotels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NightsLabel",
                table: "Hotels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tips",
                table: "Hotels",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdhanAsr",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "AdhanFajr",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "AdhanIsha",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "AdhanMaghrib",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "AdhanZuhr",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "Amenities",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "ColorClass",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "HaramDistanceText",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "HaramLatitude",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "HaramLongitude",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "IconEmoji",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "JamatAsr",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "JamatFajr",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "JamatIsha",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "JamatMaghrib",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "JamatZuhr",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "NearHaram",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "NightsLabel",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "Tips",
                table: "Hotels");
        }
    }
}
