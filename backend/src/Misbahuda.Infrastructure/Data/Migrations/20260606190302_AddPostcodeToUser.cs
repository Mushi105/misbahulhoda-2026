using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misbahuda.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPostcodeToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Postcode",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Postcode",
                table: "Users");
        }
    }
}
