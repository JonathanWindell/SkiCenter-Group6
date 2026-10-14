using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class RefactoredSkiLessons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Days",
                table: "SkiLessonSession");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "SkiLessonSession");

            migrationBuilder.DropColumn(
                name: "DurationHours",
                table: "SkiLessons");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "BookingAccommodations");

            migrationBuilder.DropColumn(
                name: "DiscountRate",
                table: "BookingAccommodations");

            migrationBuilder.DropColumn(
                name: "FinalPrice",
                table: "BookingAccommodations");

            migrationBuilder.AddColumn<string>(
                name: "Days",
                table: "SkiLessons",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "SkiLessons",
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
                name: "Days",
                table: "SkiLessons");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "SkiLessons");

            migrationBuilder.AddColumn<string>(
                name: "Days",
                table: "SkiLessonSession",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "SkiLessonSession",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DurationHours",
                table: "SkiLessons",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "BookingAccommodations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountRate",
                table: "BookingAccommodations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalPrice",
                table: "BookingAccommodations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
