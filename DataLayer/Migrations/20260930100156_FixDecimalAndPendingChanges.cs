using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class FixDecimalAndPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Rentals",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ReturnDate",
                table: "Rentals",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Rentals",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "Invoices",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "Invoices",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AddColumn<int>(
                name: "EquipmentID",
                table: "EquipmentItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SkiLessonSession",
                columns: table => new
                {
                    SkiLessonSessionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeekNumber = table.Column<int>(type: "int", nullable: false),
                    Days = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeSlot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SkiLessonID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkiLessonSession", x => x.SkiLessonSessionID);
                    table.ForeignKey(
                        name: "FK_SkiLessonSession_SkiLessons_SkiLessonID",
                        column: x => x.SkiLessonID,
                        principalTable: "SkiLessons",
                        principalColumn: "SkiLessonID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkiLessonBooking",
                columns: table => new
                {
                    SkiLessonBookingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SkiLessonSessionID = table.Column<int>(type: "int", nullable: false),
                    BookingID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkiLessonBooking", x => x.SkiLessonBookingID);
                    table.ForeignKey(
                        name: "FK_SkiLessonBooking_Bookings_BookingID",
                        column: x => x.BookingID,
                        principalTable: "Bookings",
                        principalColumn: "BookingID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SkiLessonBooking_SkiLessonSession_SkiLessonSessionID",
                        column: x => x.SkiLessonSessionID,
                        principalTable: "SkiLessonSession",
                        principalColumn: "SkiLessonSessionID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentItems_EquipmentID",
                table: "EquipmentItems",
                column: "EquipmentID");

            migrationBuilder.CreateIndex(
                name: "IX_SkiLessonBooking_BookingID",
                table: "SkiLessonBooking",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_SkiLessonBooking_SkiLessonSessionID",
                table: "SkiLessonBooking",
                column: "SkiLessonSessionID");

            migrationBuilder.CreateIndex(
                name: "IX_SkiLessonSession_SkiLessonID",
                table: "SkiLessonSession",
                column: "SkiLessonID");

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentItems_Equipment_EquipmentID",
                table: "EquipmentItems",
                column: "EquipmentID",
                principalTable: "Equipment",
                principalColumn: "EquipmentID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentItems_Equipment_EquipmentID",
                table: "EquipmentItems");

            migrationBuilder.DropTable(
                name: "SkiLessonBooking");

            migrationBuilder.DropTable(
                name: "SkiLessonSession");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentItems_EquipmentID",
                table: "EquipmentItems");

            migrationBuilder.DropColumn(
                name: "EquipmentID",
                table: "EquipmentItems");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "StartDate",
                table: "Rentals",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "ReturnDate",
                table: "Rentals",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "EndDate",
                table: "Rentals",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "DueDate",
                table: "Invoices",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "Date",
                table: "Invoices",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }
    }
}
