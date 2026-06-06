using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Misbahuda.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTourSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FeedbackSent",
                table: "Pilgrims",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FeedbackSentAt",
                table: "Pilgrims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PackageId",
                table: "Pilgrims",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TourId",
                table: "Pilgrims",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Tours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TourName = table.Column<string>(type: "text", nullable: false),
                    TourType = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsOpen = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tours", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PilgrimFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PilgrimId = table.Column<Guid>(type: "uuid", nullable: false),
                    TourId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallRating = table.Column<int>(type: "integer", nullable: false),
                    HotelRating = table.Column<int>(type: "integer", nullable: true),
                    TransportRating = table.Column<int>(type: "integer", nullable: true),
                    OrganizationRating = table.Column<int>(type: "integer", nullable: true),
                    FoodRating = table.Column<int>(type: "integer", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    Suggestions = table.Column<string>(type: "text", nullable: true),
                    WouldRecommend = table.Column<bool>(type: "boolean", nullable: false),
                    WouldReturn = table.Column<bool>(type: "boolean", nullable: false),
                    PreferredPackageNextYear = table.Column<string>(type: "text", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PilgrimFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PilgrimFeedbacks_Pilgrims_PilgrimId",
                        column: x => x.PilgrimId,
                        principalTable: "Pilgrims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PilgrimFeedbacks_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TourId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    MaxPilgrims = table.Column<int>(type: "integer", nullable: false),
                    DurationDays = table.Column<int>(type: "integer", nullable: false),
                    IncludesAirportTransfer = table.Column<bool>(type: "boolean", nullable: false),
                    IncludesBreakfast = table.Column<bool>(type: "boolean", nullable: false),
                    IncludesLunch = table.Column<bool>(type: "boolean", nullable: false),
                    IncludesDinner = table.Column<bool>(type: "boolean", nullable: false),
                    HotelInfo = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TourPackages_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pilgrims_PackageId",
                table: "Pilgrims",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Pilgrims_TourId",
                table: "Pilgrims",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_PilgrimFeedbacks_PilgrimId",
                table: "PilgrimFeedbacks",
                column: "PilgrimId");

            migrationBuilder.CreateIndex(
                name: "IX_PilgrimFeedbacks_TourId",
                table: "PilgrimFeedbacks",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPackages_TourId",
                table: "TourPackages",
                column: "TourId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pilgrims_TourPackages_PackageId",
                table: "Pilgrims",
                column: "PackageId",
                principalTable: "TourPackages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pilgrims_Tours_TourId",
                table: "Pilgrims",
                column: "TourId",
                principalTable: "Tours",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pilgrims_TourPackages_PackageId",
                table: "Pilgrims");

            migrationBuilder.DropForeignKey(
                name: "FK_Pilgrims_Tours_TourId",
                table: "Pilgrims");

            migrationBuilder.DropTable(
                name: "PilgrimFeedbacks");

            migrationBuilder.DropTable(
                name: "TourPackages");

            migrationBuilder.DropTable(
                name: "Tours");

            migrationBuilder.DropIndex(
                name: "IX_Pilgrims_PackageId",
                table: "Pilgrims");

            migrationBuilder.DropIndex(
                name: "IX_Pilgrims_TourId",
                table: "Pilgrims");

            migrationBuilder.DropColumn(
                name: "FeedbackSent",
                table: "Pilgrims");

            migrationBuilder.DropColumn(
                name: "FeedbackSentAt",
                table: "Pilgrims");

            migrationBuilder.DropColumn(
                name: "PackageId",
                table: "Pilgrims");

            migrationBuilder.DropColumn(
                name: "TourId",
                table: "Pilgrims");
        }
    }
}
