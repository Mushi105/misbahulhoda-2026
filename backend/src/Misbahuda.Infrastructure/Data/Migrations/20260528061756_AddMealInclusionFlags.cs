using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misbahuda.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMealInclusionFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BreakfastIncluded",
                table: "FinanceExpenses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DinnerIncluded",
                table: "FinanceExpenses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LunchIncluded",
                table: "FinanceExpenses",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BreakfastIncluded",
                table: "FinanceExpenses");

            migrationBuilder.DropColumn(
                name: "DinnerIncluded",
                table: "FinanceExpenses");

            migrationBuilder.DropColumn(
                name: "LunchIncluded",
                table: "FinanceExpenses");
        }
    }
}
