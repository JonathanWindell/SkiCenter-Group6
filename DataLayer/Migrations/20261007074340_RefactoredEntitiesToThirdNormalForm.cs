using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class RefactoredEntitiesToThirdNormalForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingMeetingRoom_Bookings_BookingsBookingID",
                table: "BookingMeetingRoom");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingMeetingRoom_MeetingRooms_MeetingRoomsMeetingRoomID",
                table: "BookingMeetingRoom");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Bookings_BookingID",
                table: "Invoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BookingMeetingRoom",
                table: "BookingMeetingRoom");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "MeetingRooms");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "MeetingRooms");

            migrationBuilder.DropColumn(
                name: "AmountIncl",
                table: "Invoices");

            migrationBuilder.RenameColumn(
                name: "MeetingRoomsMeetingRoomID",
                table: "BookingMeetingRoom",
                newName: "MeetingRoomID");

            migrationBuilder.RenameColumn(
                name: "BookingsBookingID",
                table: "BookingMeetingRoom",
                newName: "BookingID");

            migrationBuilder.RenameIndex(
                name: "IX_BookingMeetingRoom_MeetingRoomsMeetingRoomID",
                table: "BookingMeetingRoom",
                newName: "IX_BookingMeetingRoom_MeetingRoomID");

            migrationBuilder.AlterColumn<int>(
                name: "BookingID",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BookingMeetingRoomID",
                table: "BookingMeetingRoom",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "BookingMeetingRoom",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "BookingMeetingRoom",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_BookingMeetingRoom",
                table: "BookingMeetingRoom",
                column: "BookingMeetingRoomID");

            migrationBuilder.CreateIndex(
                name: "IX_BookingMeetingRoom_BookingID",
                table: "BookingMeetingRoom",
                column: "BookingID");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingMeetingRoom_Bookings_BookingID",
                table: "BookingMeetingRoom",
                column: "BookingID",
                principalTable: "Bookings",
                principalColumn: "BookingID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BookingMeetingRoom_MeetingRooms_MeetingRoomID",
                table: "BookingMeetingRoom",
                column: "MeetingRoomID",
                principalTable: "MeetingRooms",
                principalColumn: "MeetingRoomID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Bookings_BookingID",
                table: "Invoices",
                column: "BookingID",
                principalTable: "Bookings",
                principalColumn: "BookingID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingMeetingRoom_Bookings_BookingID",
                table: "BookingMeetingRoom");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingMeetingRoom_MeetingRooms_MeetingRoomID",
                table: "BookingMeetingRoom");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Bookings_BookingID",
                table: "Invoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BookingMeetingRoom",
                table: "BookingMeetingRoom");

            migrationBuilder.DropIndex(
                name: "IX_BookingMeetingRoom_BookingID",
                table: "BookingMeetingRoom");

            migrationBuilder.DropColumn(
                name: "BookingMeetingRoomID",
                table: "BookingMeetingRoom");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "BookingMeetingRoom");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "BookingMeetingRoom");

            migrationBuilder.RenameColumn(
                name: "MeetingRoomID",
                table: "BookingMeetingRoom",
                newName: "MeetingRoomsMeetingRoomID");

            migrationBuilder.RenameColumn(
                name: "BookingID",
                table: "BookingMeetingRoom",
                newName: "BookingsBookingID");

            migrationBuilder.RenameIndex(
                name: "IX_BookingMeetingRoom_MeetingRoomID",
                table: "BookingMeetingRoom",
                newName: "IX_BookingMeetingRoom_MeetingRoomsMeetingRoomID");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "MeetingRooms",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "MeetingRooms",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<int>(
                name: "BookingID",
                table: "Invoices",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountIncl",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "PK_BookingMeetingRoom",
                table: "BookingMeetingRoom",
                columns: new[] { "BookingsBookingID", "MeetingRoomsMeetingRoomID" });

            migrationBuilder.AddForeignKey(
                name: "FK_BookingMeetingRoom_Bookings_BookingsBookingID",
                table: "BookingMeetingRoom",
                column: "BookingsBookingID",
                principalTable: "Bookings",
                principalColumn: "BookingID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BookingMeetingRoom_MeetingRooms_MeetingRoomsMeetingRoomID",
                table: "BookingMeetingRoom",
                column: "MeetingRoomsMeetingRoomID",
                principalTable: "MeetingRooms",
                principalColumn: "MeetingRoomID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Bookings_BookingID",
                table: "Invoices",
                column: "BookingID",
                principalTable: "Bookings",
                principalColumn: "BookingID");
        }
    }
}
