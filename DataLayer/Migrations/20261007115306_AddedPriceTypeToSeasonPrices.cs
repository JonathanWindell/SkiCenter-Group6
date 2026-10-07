using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddedPriceTypeToSeasonPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SeasonPrices_AccommodationType_AccommodationTypeID",
                table: "SeasonPrices");

            migrationBuilder.RenameColumn(
                name: "AccommodationType",
                table: "SeasonPrices",
                newName: "PriceType");

            migrationBuilder.AlterColumn<int>(
                name: "AccommodationTypeID",
                table: "SeasonPrices",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SeasonPrices_AccommodationType_AccommodationTypeID",
                table: "SeasonPrices",
                column: "AccommodationTypeID",
                principalTable: "AccommodationType",
                principalColumn: "AccommodationTypeID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SeasonPrices_AccommodationType_AccommodationTypeID",
                table: "SeasonPrices");

            migrationBuilder.RenameColumn(
                name: "PriceType",
                table: "SeasonPrices",
                newName: "AccommodationType");

            migrationBuilder.AlterColumn<int>(
                name: "AccommodationTypeID",
                table: "SeasonPrices",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_SeasonPrices_AccommodationType_AccommodationTypeID",
                table: "SeasonPrices",
                column: "AccommodationTypeID",
                principalTable: "AccommodationType",
                principalColumn: "AccommodationTypeID");
        }
    }
}
