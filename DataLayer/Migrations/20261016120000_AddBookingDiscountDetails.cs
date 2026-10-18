using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <summary>
    /// Stores the accommodation list price and the applied discount on each booking,
    /// so invoicing exports can show exactly which discount was given.
    /// </summary>
    [DbContext(typeof(SkiCenterDbContext))]
    [Migration("20261016120000_AddBookingDiscountDetails")]
    public partial class AddBookingDiscountDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AccommodationListPrice",
                table: "Bookings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountRate",
                table: "Bookings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Bookings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccommodationListPrice",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DiscountRate",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Bookings");
        }
    }
}
