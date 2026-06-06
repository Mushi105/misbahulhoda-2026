using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misbahuda.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakePassportNumberNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pilgrims_PassportNumber",
                table: "Pilgrims");

            migrationBuilder.AlterColumn<string>(
                name: "PassportNumber",
                table: "Pilgrims",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_Pilgrims_PassportNumber",
                table: "Pilgrims",
                column: "PassportNumber",
                unique: true,
                filter: "\"PassportNumber\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pilgrims_PassportNumber",
                table: "Pilgrims");

            migrationBuilder.AlterColumn<string>(
                name: "PassportNumber",
                table: "Pilgrims",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pilgrims_PassportNumber",
                table: "Pilgrims",
                column: "PassportNumber",
                unique: true);
        }
    }
}
