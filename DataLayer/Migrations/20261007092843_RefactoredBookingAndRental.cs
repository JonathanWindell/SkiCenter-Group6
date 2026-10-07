using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class RefactoredBookingAndRental : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sum",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Bookings");

            migrationBuilder.AddColumn<int>(
                name: "BookingStatus",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CheckInStatus",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookingStatus",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CheckInStatus",
                table: "Bookings");

            migrationBuilder.AddColumn<decimal>(
                name: "Sum",
                table: "Rentals",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
