using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <summary>
    /// Hand-written migration. Stores the total price of a rental and whether the customer's
    /// driver's license was verified (required for snowmobile rentals).
    /// </summary>
    [DbContext(typeof(SkiCenterDbContext))]
    [Migration("20261013120000_AddRentalPriceAndLicenseCheck")]
    public partial class AddRentalPriceAndLicenseCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalPrice",
                table: "Rentals",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "LicenseVerified",
                table: "Rentals",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalPrice",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "LicenseVerified",
                table: "Rentals");
        }
    }
}
