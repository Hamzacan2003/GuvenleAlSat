using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GuvenleAlSat.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddRealEstateDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RealEstateDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrossSquareMeters = table.Column<int>(type: "integer", nullable: true),
                    NetSquareMeters = table.Column<int>(type: "integer", nullable: true),
                    RoomCount = table.Column<string>(type: "text", nullable: true),
                    BuildingAge = table.Column<int>(type: "integer", nullable: true),
                    FloorLocation = table.Column<int>(type: "integer", nullable: true),
                    TotalFloors = table.Column<int>(type: "integer", nullable: true),
                    HeatingType = table.Column<string>(type: "text", nullable: true),
                    BathroomCount = table.Column<int>(type: "integer", nullable: true),
                    HasBalcony = table.Column<bool>(type: "boolean", nullable: false),
                    IsFurnished = table.Column<bool>(type: "boolean", nullable: false),
                    InSite = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RealEstateDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RealEstateDetails_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RealEstateDetails_ListingId",
                table: "RealEstateDetails",
                column: "ListingId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RealEstateDetails");
        }
    }
}
