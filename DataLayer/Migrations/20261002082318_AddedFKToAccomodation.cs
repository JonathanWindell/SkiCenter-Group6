using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddedFKToAccomodation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeasonPriceID",
                table: "Accommodations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Accommodations_SeasonPriceID",
                table: "Accommodations",
                column: "SeasonPriceID");

            migrationBuilder.AddForeignKey(
                name: "FK_Accommodations_SeasonPrices_SeasonPriceID",
                table: "Accommodations",
                column: "SeasonPriceID",
                principalTable: "SeasonPrices",
                principalColumn: "SeasonPriceID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accommodations_SeasonPrices_SeasonPriceID",
                table: "Accommodations");

            migrationBuilder.DropIndex(
                name: "IX_Accommodations_SeasonPriceID",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "SeasonPriceID",
                table: "Accommodations");
        }
    }
}
